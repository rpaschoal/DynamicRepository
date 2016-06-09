using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;
using DynamicRepository.Filter;
using LinqKit;
using DynamicRepository.Reflection;
using System.Data.Entity;

namespace DynamicRepository.NET
{
    /// <summary>
    /// Base repository for persistency model CRUD and advanced filtering operations.
    /// </summary>
    /// <typeparam name="Key">The key type of current entity type. For composed primary keys use a new class type definition or an <see cref="object[]"/> array.</typeparam>
    /// <typeparam name="Entity">The type of the entity being persisted or retrieved.</typeparam>
    public abstract class Repository<Key, Entity> : IRepository<Key, Entity> where Entity : class, new()
    {
        /// <summary>
        /// Current EF DBContext instance.
        /// </summary>
        protected virtual DbContext Context { get; set; }

        /// <summary>
        /// DBSet of <see cref="Entity"/> extracted from <see cref="Context"/>.
        /// </summary>
        protected internal DbSet<Entity> DbSet;

        /// <summary>
        /// Default constructor of main repository. 
        /// Required dependencies are injected.
        /// </summary>
        /// <param name="context">Current EF context.</param>
        /// <param name="account">Current request account identification.</param>
        public Repository(DbContext context)
        {
            Context = context;

            // Configures current entity DB Set which is being manipulated
            DbSet = context.Set<Entity>();
        }

        /// <summary>
        /// Returns an instance of non-filtered IQueryable of all items in a DBSet.
        /// </summary>
        /// <returns>IQueryable instance of type <see cref="Entity"/></returns>
        internal virtual IQueryable<Entity> List()
        {
            return DbSet;
        }

        /// <summary>
        /// Gets an entity instance based on its <see cref="Key"/>.
        /// </summary>
        /// <param name="key">The desired entity key value.</param>
        /// <returns>Persisted entity if found, otherwise NULL.</returns>
        public virtual Entity Get(Key key)
        {
            if (key is Array)
            {
                // This is to handle entity framework find by composite key
                return DbSet.Find((key as IEnumerable).Cast<object>().ToArray());
            }
            else
            {
                return DbSet.Find(key);
            }
        }

        /// <summary>
        /// Persists a new entity model.
        /// </summary>
        /// <param name="entity">The new <see cref="Entity"/> instance to be persisted.</param>
        public virtual void Insert(Entity entity)
        {
            DbSet.Add(entity);
            Context.SaveChanges();
        }

        /// <summary>
        /// Updates an existing persisted entity.
        /// </summary>
        /// <param name="entityToUpdate">The <see cref="Entity"/> instance to be updated.</param>
        public virtual void Update(Entity entityToUpdate)
        {
            Context.Entry(entityToUpdate).State = EntityState.Modified;
            Context.SaveChanges();
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="id">The primary key of the <see cref="Entity"/> to be deleted.</param>
        public virtual void Delete(Key id)
        {
            Delete(this.Get(id));
            Context.SaveChanges();
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="entityToDelete">The <see cref="Entity"/> instance to be deleted.</param>
        public void Delete(Entity entityToDelete)
        {
            if (entityToDelete != null)
            {
                DbSet.Remove(entityToDelete);
                Context.SaveChanges();
            }
        }

        /// <summary>
        /// Filter, order and join the current entity based on criterias supplied as parameters.
        /// </summary>
        /// <param name="filter">Expression which supplies all desired filters.</param>
        /// <param name="orderBy">Projetion to order the result.</param>
        /// <param name="includeProperties">
        /// Navigation properties that should be included on this query result. 
        /// Ignore this if you have lazy loading enabled.
        /// </param>
        /// <returns>Fullfilled collection based on the criteria.</returns>
        public IEnumerable<Entity> List(
            Expression<Func<Entity, bool>> filter = null,
            Func<IQueryable<Entity>, IOrderedQueryable<Entity>> orderBy = null,
            params string[] includeProperties)
        {
            IQueryable<Entity> query = DbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }

            if (orderBy != null)
            {
                return orderBy(query).ToList();
            }
            else
            {
                return query.ToList();
            }
        }

        /// <summary>
        /// Returns <see cref="IQueryable"/> for consumers to shape queries as they need to.
        /// </summary>
        /// <returns>
        /// Plain DbSet as Queryable.
        /// </returns>
        protected internal IQueryable<Entity> AsQueryable()
        {
            return DbSet.AsQueryable();
        }

        #region Paged Data Source Implemantation and Definitions

        /// <summary>
        /// Returns a collection that can be paged on consumers (API/UI).
        /// </summary>
        /// <param name="settings">Settings for the search.</param>
        /// <param name="accountScoped">If true defines that the filter should apply only to current pipeline accountID. Defaults to TRUE.</param>
        /// <returns>Filled PagedDataSource instance.</returns>
        public IPagedDataSourceResult<Entity> GetPagedDataSource(PagedDataSourceSettings settings)
        {
            try
            {
                IQueryable<Entity> pagedDataSourceQuery = (IQueryable<Entity>)this.List();

                // Adds conditions which applies to Account level filter. Most useful for security branch checks.
                var preConditionExpression = AddPreConditionsToPagedDataSourceFilter(settings);
                if (preConditionExpression != null)
                {
                    pagedDataSourceQuery = pagedDataSourceQuery.Where(preConditionExpression);
                }

                // Adds composed filter to the query here (This is the default filter inspector bult-in for the search).
                pagedDataSourceQuery = pagedDataSourceQuery.Where(DefaultPagedDataSourceFilter(settings));

                // Generates the order clause based on supplied parameters
                if (settings.Order != null && settings.Order.Count > 0)
                {
                    foreach (var o in settings.Order)
                    {
                        pagedDataSourceQuery = pagedDataSourceQuery.OrderBy(o.Key + " " + o.Value.ToString());
                    }
                }
                else
                {
                    var propCollection = typeof(Entity).GetTypeInfo().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x => !typeof(IEnumerable).IsAssignableFrom(x.PropertyType)).ToList();

                    if (propCollection.Count() > 0)
                    {
                        var firstFieldOfEntity = propCollection[0].Name;
                        pagedDataSourceQuery = pagedDataSourceQuery.OrderBy(firstFieldOfEntity + " " + SortOrderEnum.DESC.ToString());
                    }
                    else
                    {
                        throw new Exception($"The supplied Entity {nameof(Entity)} has no public properties, therefore the method can't continue the filter operation.");
                    }
                }

                // Shapes final paged result model.
                return new PagedDataSourceResult<Entity>(pagedDataSourceQuery.AsExpandable().Count())
                {
                    Result = pagedDataSourceQuery.Skip((settings.Page - 1) * settings.TotalPerPage).Take(settings.TotalPerPage).AsExpandable().ToList()
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"There was an error paging the desired datasource for entity: {nameof(Entity)}. Details: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds default filter mechanism to GetPagedDataSource method.
        /// </summary>
        /// <remarks>
        /// This method allows multi-navigation property filter as long as they are not collections.
        /// It also supports collection BUT the collection needs to be the immediate first level of navigation property, and you can't use more than one depth.
        /// </remarks>
        /// <param name="settings">Current filter settings supplied by the consumer.</param>
        /// <returns>Expression to be embedded to the IQueryable filter instance.</returns>
        private Expression<Func<Entity, bool>> DefaultPagedDataSourceFilter(PagedDataSourceSettings settings)
        {
            bool firstExecution = true;
            var queryLinq = string.Empty;

            if (settings.Filter != null)
            {
                foreach (var pFilter in settings.Filter.Where(x => !String.IsNullOrEmpty(x.Property) && !String.IsNullOrEmpty(x.Value)))
                {
                    // Means we found that property in the entity model. Otherwise we should ignore or face with an Exception from IQueryable.
                    // We are also checking to see if we are not querying directly to the collection holder.
                    int collectionPathTotal;
                    var propInfo = new Entity().GetNestedPropInfo(pFilter.Property, out collectionPathTotal);

                    if (collectionPathTotal > 1)
                    {
                        throw new Exception($"{nameof(this.GetPagedDataSource)} method does not support more than one nested collection depth filter. Please try using {nameof(AddExtraPagedDataSourceFilter)} for more advanced queries.");
                    }
                    else if (collectionPathTotal == 1)
                    {
                        var firstLevelPropInfo = new Entity().GetNestedPropInfo(pFilter.Property.Split('.')[0]).PropertyType;

                        // Checks if it is not the first level of depth that holds the collection
                        if (firstLevelPropInfo == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(firstLevelPropInfo))
                        {
                            // Seriously, this is too much innovation if it happens.
                            throw new Exception($"{nameof(this.GetPagedDataSource)} method does not support to filter in collections which are not in the first level of depth. . Please try using {nameof(AddExtraPagedDataSourceFilter)} for more advanced queries.");
                        }
                    }

                    // Apparently String implements IEnumerable, since it is a collection of chars
                    if (propInfo != null && (propInfo.PropertyType == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(propInfo.PropertyType)))
                    {
                        if (collectionPathTotal == 0)
                        {
                            if (pFilter.IsExactMatch)
                            {
                                queryLinq += (firstExecution ? string.Empty : " " + pFilter.Conjunction + " ") + pFilter.Property + ".ToString().ToUpper() == \"" + pFilter.Value.ToUpper() + "\"";
                            }
                            else
                            {
                                queryLinq += (firstExecution ? string.Empty : " " + pFilter.Conjunction + " ") + pFilter.Property + ".ToString().ToUpper().Contains(\"" + pFilter.Value.ToUpper() + "\")";
                            }

                            firstExecution = false;
                        }
                        else
                        {
                            // Only supports if it is immediately the first level. We checked this above =)
                            var navigationPropertyCollection = pFilter.Property.Split('.')[0];

                            // Sub collection filter LINQ
                            if (pFilter.IsExactMatch)
                            {
                                queryLinq += (firstExecution ? string.Empty : " " + pFilter.Conjunction + " ") + navigationPropertyCollection + ".Where(" + pFilter.Property.Remove(0, navigationPropertyCollection.Length + 1) + ".ToString().ToUpper() == \"" + pFilter.Value.ToUpper() + "\").Any()";
                            }
                            else
                            {
                                queryLinq += (firstExecution ? string.Empty : " " + pFilter.Conjunction + " ") + navigationPropertyCollection + ".Where(" + pFilter.Property.Remove(0, navigationPropertyCollection.Length + 1) + ".ToString().ToUpper().Contains(\"" + pFilter.Value.ToUpper() + "\")).Any()";
                            }

                            firstExecution = false;
                        }
                    }
                }
            }

            return MergeFilters(settings, queryLinq, settings.SearchInALL);
        }

        /// <summary>
        /// Adds extra filter to PagedDataSource method.
        /// </summary>
        /// <remarks>
        /// Override this method in <see cref="Repository{Key, Entity}{Key, Entity}"/> implementation 
        /// if you want to add custom filter to your paged data source.
        /// </remarks>
        /// <param name="settings">Current filter settings supplied by the consumer.</param>
        /// <returns>Expression to be embedded to the IQueryable filter instance.</returns>
        protected virtual Expression<Func<Entity, bool>> AddExtraPagedDataSourceFilter(PagedDataSourceSettings settings)
        {
            // Needs to be overriden by devs to add behavior to this. 
            // Change the injected filter on concrete repositories.
            return null;
        }

        /// <summary>
        /// Adds precondition global filters to paged data source.
        /// Rely on this if you want to add security filters.
        /// </summary>
        /// <remarks>
        /// Override this method in <see cref="Repository{Key, Entity}{Key, Entity}"/> implementation 
        /// if you want to add pre conditions global filters to your paged data source.
        /// </remarks>
        /// <param name="settings">Current filter settings supplied by the consumer.</param>
        /// <returns>Expression to be embedded to the IQueryable filter instance.</returns>
        protected virtual Expression<Func<Entity, bool>> AddPreConditionsToPagedDataSourceFilter(PagedDataSourceSettings settings)
        {
            // Needs to be overriden by devs to add behavior to this.
            return null;
        }

        /// <summary>
        /// Parses an IQueryable instance to string lambda expression.
        /// </summary>
        /// <param name="query">The IQueryable instance to be parsed.</param>
        /// <returns>The string lambda expression.</returns>
        private Expression<Func<Entity, bool>> MergeFilters(PagedDataSourceSettings settings, string expression, bool isAllSearch = false)
        {
            var expression1 = !String.IsNullOrEmpty(expression) ? System.Linq.Dynamic.DynamicExpression.ParseLambda<Entity, bool>(expression, null) : null;
            var expression2 = AddExtraPagedDataSourceFilter(settings);

            if (expression1 == null && expression2 == null)
            {
                return x => 1 == 1;
            }
            else if (expression1 != null && expression2 != null)
            {
                if (isAllSearch)
                {
                    return PredicateBuilder.Or(expression1, expression2);
                }
                else
                {
                    return PredicateBuilder.And(expression1, expression2);
                }
            }
            else if (expression1 == null)
            {
                return expression2;
            }
            else
            {
                return expression1;
            }
        }

        #endregion
    }
}
