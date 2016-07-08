using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;
using DynamicRepository.Filter;
using LinqKit;
using DynamicRepository.Extensions;
using System.Data.Entity;
using System.Linq.Dynamic.Core;

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
        /// Returns a collection of data results that can be paged.
        /// </summary>
        /// <param name="settings">Settings for the search.</param>
        /// <returns>Filled PagedDataSource instance.</returns>
        public IPagedDataSourceResult<Entity> GetPagedDataSource(PagedDataSourceSettings settings)
        {
            try
            {
                IQueryable<Entity> pagedDataSourceQuery = (IQueryable<Entity>)this.List();

                // Adds conditions which applies to Account level filter. Most useful for security branch checks.
                var preConditionExpression = AddPreConditionsPagedDataSourceFilter(settings);
                if (preConditionExpression != null)
                {
                    pagedDataSourceQuery = pagedDataSourceQuery.Where(preConditionExpression);
                }

                // Adds composed filter to the query here (This is the default filter inspector bult-in for the search).
                // This is a merge result from default query engine + customized queries from devs (AddExtraPagedDataSourceFilter method).
                pagedDataSourceQuery = pagedDataSourceQuery.Where(MergeFilters(settings, DefaultPagedDataSourceFilter(settings), AddExtraPagedDataSourceFilter(settings), settings.SearchInALL));

                // Adds sorting capabilities
                pagedDataSourceQuery = this.AddSorting(pagedDataSourceQuery, settings);

                // Total number of records regardless of paging.
                var totalRecordsInDB = pagedDataSourceQuery.AsExpandable().Count();

                // Shapes final result model
                return new PagedDataSourceResult<Entity>(pagedDataSourceQuery.AsExpandable().Count())
                {
                    Result = pagedDataSourceQuery.Skip((settings.Page - 1) * settings.TotalPerPage).Take(settings.TotalPerPage).AsExpandable().ToList()
                };

                // TODO: Make in memory callback work on later time.
                //return pagedDataSourceQuery.Skip((settings.Page - 1) * settings.TotalPerPage).Take(settings.TotalPerPage).AsExpandable().ToWrapperPaged(totalRecordsInDB, (p) => InMemoryOperationsCallback(p, settings));
            }
            catch (Exception ex)
            {
                throw new Exception($"There was an error paging the desired datasource for entity: {nameof(Entity)}. Details: {ex.Message}");
            }
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
        protected virtual Expression<Func<Entity, bool>> AddPreConditionsPagedDataSourceFilter(PagedDataSourceSettings settings)
        {
            // Needs to be overriden by devs to add behavior to this.
            return null;
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

            if (settings.Filter != null && settings.Filter.Count > 0)
            {
                var validFilterSettings = settings.Filter.Where(x => !String.IsNullOrEmpty(x.Property) && !String.IsNullOrEmpty(x.Value)).GroupBy(x => x.Property).Select(y => y.FirstOrDefault());

                foreach (var pFilter in validFilterSettings)
                {
                    int collectionPathTotal = 0;
                    var propInfo = this.GetValidatedPropertyInfo(pFilter.Property, out collectionPathTotal);

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

            // Returns current default query as expression.
            return queryLinq.ParseLambda<Entity>();
        }

        /// <summary>
        /// Adds default sorting mechanism to GetPagedDataSource method.
        /// </summary>
        /// <remarks>
        /// This method allows multi-navigation property filter as long as they are not collections.
        /// It also supports collection BUT the collection needs to be the immediate first level of navigation property, and you can't use more than one depth.
        /// 
        /// - The input IQueryable is being returned. Seems if you try to apply changes by reference, you don't get it outside of this method. May be implicit LINQ behavior.
        /// </remarks>
        /// <param name="settings">Current sorting settings supplied by the consumer.</param>
        /// <returns>Expression to be embedded to the IQueryable instance.</returns>
        private IQueryable<Entity> AddSorting(IQueryable<Entity> pagedDataSourceQuery, PagedDataSourceSettings settings)
        {
            bool noFilterApplied = true;

            // Generates the order clause based on supplied parameters
            if (settings.Order != null && settings.Order.Count > 0)
            {
                var validOrderSettings = settings.Order.Where(x => !String.IsNullOrEmpty(x.Property) && String.IsNullOrEmpty(x.PostQuerySortingPath)).GroupBy(x => x.Property).Select(y => y.FirstOrDefault());

                foreach (var o in validOrderSettings)
                {
                    int collectionPathTotal = 0;
                    var propInfo = this.GetValidatedPropertyInfo(o.Property, out collectionPathTotal);

                    // Apparently String implements IEnumerable, since it is a collection of chars
                    if (propInfo != null && (propInfo.PropertyType == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(propInfo.PropertyType)))
                    {
                        // Just applying DB filters to non collection related properties.
                        if (collectionPathTotal == 0)
                        {
                            if (noFilterApplied)
                                noFilterApplied = false;

                            pagedDataSourceQuery = pagedDataSourceQuery.OrderBy(o.Property + " " + o.Order.ToString());
                        }
                        else
                        {
                            throw new Exception($"Database translated queries for collection properties are not supported. Please use method: {nameof(this.PostQueryFilter)}.");
                        }
                    }
                }
            }

            // If there is no sorting configured, we need to add a default fallback one, which we will use the first property. Without this can't use LINQ Skip/Take
            if (settings.Order == null || settings.Order.Count == 0 || noFilterApplied)
            {
                var propCollection = typeof(Entity).GetTypeInfo().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x => !typeof(IEnumerable).IsAssignableFrom(x.PropertyType)).ToList();

                if (propCollection.Count() > 0)
                {
                    var firstFieldOfEntity = propCollection[0].Name;
                    pagedDataSourceQuery = pagedDataSourceQuery.OrderBy(firstFieldOfEntity + " " + SortOrderEnum.DESC.ToString());
                }
                else
                {
                    throw new Exception($"The supplied Entity {nameof(Entity)} has no public properties, therefore the method can't continue the sorting operation.");
                }
            }

            return pagedDataSourceQuery;
        }

        /// <summary>
        /// Merges two filters into one final query.
        /// </summary>
        /// <param name="query">The IQueryable instance to be parsed.</param>
        /// <returns>The string lambda expression.</returns>
        private Expression<Func<Entity, bool>> MergeFilters(PagedDataSourceSettings settings,
            Expression<Func<Entity, bool>> expressionLeft,
            Expression<Func<Entity, bool>> expressionRight,
            bool isAllSearch = false)
        {
            if (expressionLeft == null && expressionRight == null)
            {
                return x => 1 == 1;
            }
            else if (expressionLeft != null && expressionRight != null)
            {
                if (isAllSearch)
                {
                    return PredicateBuilder.Or(expressionLeft, expressionRight);
                }
                else
                {
                    return PredicateBuilder.And(expressionLeft, expressionRight);
                }
            }
            else if (expressionLeft == null)
            {
                return expressionRight;
            }
            else
            {
                return expressionLeft;
            }
        }

        /// <summary>
        /// Filters the final paged result AFTER the projection was executed in database by adapters.
        /// </summary>
        /// <remarks>
        /// This method is useful for children collection filter. The only way to accomplish through LINQ.
        /// </remarks>
        private IList PostQueryCallbacksInvoker(IList fetchedResult, PagedDataSourceSettings settings)
        {
            fetchedResult = this.PostQueryFilter(fetchedResult, settings);
            fetchedResult = this.PostQuerySort(fetchedResult, settings);

            return fetchedResult;
        }

        /// <summary>
        /// Applies filter to inner collections of a query result set from database.
        /// This is applied as a memory LINQ To Objects filter.
        /// </summary>
        /// <param name="fetchedResult">This is the return from EF query after going to DB.</param>
        /// <param name="settings">Paged data source settings.</param>
        /// <returns>Filtered collection result.</returns>
        private IList PostQueryFilter(IList fetchedResult, PagedDataSourceSettings settings)
        {
            if (settings.Filter != null && settings.Filter.Count > 0 && !settings.SearchInALL)
            {
                var validFilterSettings = settings.Filter
                                                  .Where(x => !String.IsNullOrEmpty(x.Property) && !String.IsNullOrEmpty(x.Value) && !String.IsNullOrEmpty(x.PostQueryFilterPath))
                                                  .GroupBy(x => x.Property)
                                                  .Select(y => y.FirstOrDefault());

                if (validFilterSettings.Count() > 0)
                {
                    foreach (var result in fetchedResult)
                    {
                        string bufferedNavigationProperty = string.Empty;
                        bool firstExecution = true;
                        var queryLinq = string.Empty;

                        foreach (var pFilter in validFilterSettings)
                        {
                            // This allows piping for DTO in memory filtering paths.
                            var pipes = pFilter.PostQueryFilterPath.Split('|');
                            bool piped = false; // Set this to true in the end if we run more than one pipe.
                            string pipedQuery = "";

                            foreach (var pipe in pipes)
                            {
                                // Only supports if it is immediately the first level. We checked this above =)
                                var navigationPropertyCollection = pipe.Split('.')[0];

                                // We are buffering the query, but if the property has changed, then we will execute and replace the value inMemory and move on
                                if (!firstExecution && bufferedNavigationProperty != navigationPropertyCollection)
                                {
                                    result.ReplaceCollectionInstance(bufferedNavigationProperty, queryLinq);

                                    // Assign brand new values to move on as a new. Resetting here since seems the collection property CHANGED.
                                    queryLinq = string.Empty;
                                    firstExecution = true;
                                    pipedQuery = string.Empty;
                                }

                                bufferedNavigationProperty = navigationPropertyCollection;
                                int collectionPathTotal = 0;
                                var propInfo = result.GetNestedPropInfo(navigationPropertyCollection, out collectionPathTotal);

                                // Apparently String implements IEnumerable, since it is a collection of chars
                                if (propInfo != null && (propInfo.PropertyType != typeof(string) || typeof(IEnumerable).IsAssignableFrom(propInfo.PropertyType)))
                                {
                                    // Sub collection filter LINQ
                                    if (pFilter.IsExactMatch)
                                    {
                                        pipedQuery += (!piped ? string.Empty : " OR ") + pipe.Remove(0, navigationPropertyCollection.Length + 1) + ".ToString().ToUpper() == \"" + pFilter.Value.ToUpper() + "\"";
                                    }
                                    else
                                    {
                                        pipedQuery += (!piped ? string.Empty : " OR ") + pipe.Remove(0, navigationPropertyCollection.Length + 1) + ".ToString().ToUpper().Contains(\"" + pFilter.Value.ToUpper() + "\")";
                                    }
                                }

                                piped = true;
                            }

                            if (!String.IsNullOrEmpty(pipedQuery))
                                queryLinq += (firstExecution ? string.Empty : " " + pFilter.Conjunction + " ") + "(" + pipedQuery + ")";

                            firstExecution = false;
                        }

                        result.ReplaceCollectionInstance(bufferedNavigationProperty, queryLinq);
                    }
                }
            }

            return fetchedResult;
        }

        /// <summary>
        /// Applies in memory sorting to IList.
        /// </summary>
        /// <param name="fetchedResult">This is the return from EF query after going to DB.</param>
        /// <param name="settings">Paged data source settings.</param>
        /// <returns>Sorted collection result.</returns>
        private IList PostQuerySort(IList fetchedResult, PagedDataSourceSettings settings)
        {
            // Generates the order clause based on supplied parameters
            if (settings.Order != null && settings.Order.Count > 0)
            {
                var validOrderSettings = settings.Order.Where(x => !String.IsNullOrEmpty(x.Property) && !String.IsNullOrEmpty(x.PostQuerySortingPath)).GroupBy(x => x.Property).Select(y => y.FirstOrDefault());

                foreach (var o in validOrderSettings)
                {
                    foreach (var result in fetchedResult)
                    {
                        // Only supports if it is immediately the first level. We checked this above =)
                        var navigationPropertyCollection = o.PostQuerySortingPath.Split('.')[0];

                        int collectionPathTotal = 0;
                        var propInfo = result.GetNestedPropInfo(navigationPropertyCollection, out collectionPathTotal);

                        // Apparently String implements IEnumerable, since it is a collection of chars
                        if (propInfo != null && (propInfo.PropertyType != typeof(string) || typeof(IEnumerable).IsAssignableFrom(propInfo.PropertyType)))
                        {
                            // Gets the property reference
                            var collectionProp = result.GetPropValue(navigationPropertyCollection);

                            if (typeof(IQueryable).IsAssignableFrom(collectionProp.GetType()))
                            {
                                // Applies filter to the IQueryable since it was inMemory Filtered.
                                collectionProp = ((IQueryable)collectionProp).OrderBy(o.PostQuerySortingPath.Substring(navigationPropertyCollection.Length + 1) + " " + o.Order.ToString());

                                // Filter in memory data here
                                result.SetPropValue(navigationPropertyCollection, collectionProp, true);
                            }
                            else
                            {
                                // Applies filter to the nested collection within the main entity.
                                collectionProp = ((IList)collectionProp).AsQueryable().OrderBy(o.PostQuerySortingPath.Substring(navigationPropertyCollection.Length + 1) + " " + o.Order.ToString());

                                // Filter in memory data here
                                result.SetPropValue(navigationPropertyCollection, collectionProp, true);
                            }
                        }
                    }
                }
            }

            return fetchedResult;
        }

        /// <summary>
        /// Gets <see cref="Entity"/> property validated.
        /// </summary>
        /// <param name="propertyPath">The property by dot notation path. Eg: Legs.Aircraft</param>
        /// <param name="collectionPathTotal">The number of collections found in depth on this path.</param>
        /// <returns>Reflection time property information.</returns>
        private PropertyInfo GetValidatedPropertyInfo(string propertyPath, out int collectionPathTotal)
        {
            // Means we found that property in the entity model. Otherwise we should ignore or face with an Exception from IQueryable.
            // We are also checking to see if we are not querying directly to the collection holder.
            var propInfo = new Entity().GetNestedPropInfo(propertyPath, out collectionPathTotal);

            if (collectionPathTotal > 1)
            {
                throw new Exception($"{nameof(this.GetPagedDataSource)} method does not support more than one nested collection depth filter. Please try using {nameof(AddExtraPagedDataSourceFilter)} for more advanced queries.");
            }
            else if (collectionPathTotal == 1)
            {
                var firstLevelPropInfo = new Entity().GetNestedPropInfo(propertyPath.Split('.')[0]).PropertyType;

                // Checks if it is not the first level of depth that holds the collection
                if (firstLevelPropInfo == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(firstLevelPropInfo))
                {
                    // Seriously, this is too much innovation if it happens.
                    throw new Exception($"{nameof(this.GetPagedDataSource)} method does not support to filter in collections which are not in the first level of depth. . Please try using {nameof(AddExtraPagedDataSourceFilter)} for more advanced queries.");
                }
            }

            return propInfo;
        }

        #endregion
    }
}
