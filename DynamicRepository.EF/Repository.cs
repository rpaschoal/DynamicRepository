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

namespace DynamicRepository.EF
{
    /// <summary>
    /// Base repository for persistency model CRUD and advanced filtering operations.
    /// </summary>
    /// <typeparam name="Key">The key type of current entity type. For composed primary keys use a new class type definition or an <see cref="object[]"/> array.</typeparam>
    /// <typeparam name="Entity">The type of the entity being persisted or retrieved.</typeparam>
    public abstract class Repository<Key, Entity> : IRepository<Key, Entity> where Entity : class, new()
    {
        private DataPager<Key, Entity> _dataSourcePager;

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

            _dataSourcePager = new DataPager<Key, Entity>();
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
        }

        /// <summary>
        /// Updates an existing persisted entity.
        /// </summary>
        /// <param name="entityToUpdate">The <see cref="Entity"/> instance to be updated.</param>
        public virtual void Update(Entity entityToUpdate)
        {
            Context.Entry(entityToUpdate).State = EntityState.Modified;
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="id">The primary key of the <see cref="Entity"/> to be deleted.</param>
        public virtual void Delete(Key id)
        {
            Delete(this.Get(id));
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
            }
        }

        /// <summary>
        /// Returns all entries of this entity.
        /// </summary>
        public IEnumerable<Entity> ListAll()
        {
            return this.DbSet.ToList();
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

        /// <summary>
        /// Returns a collection of data results that can be paged.
        /// </summary>
        /// <param name="settings">Settings for the search.</param>
        /// <returns>Filled PagedData instance.</returns>
        public IPagedDataResult<Entity> GetPagedData(PagedDataSettings settings)
        {
            return _dataSourcePager.GetPagedData((IQueryable<Entity>)this.List(), settings, this.AddPreConditionsPagedDataFilter(settings), this.AddExtraPagedDataFilter(settings));
        }

        /// <summary>
        /// Adds extra filter to PagedData method.
        /// </summary>
        /// <remarks>
        /// Override this method in <see cref="Repository{Key, Entity}{Key, Entity}"/> implementation 
        /// if you want to add custom filter to your paged data source.
        /// </remarks>
        /// <param name="settings">Current filter settings supplied by the consumer.</param>
        /// <returns>Expression to be embedded to the IQueryable filter instance.</returns>
        protected virtual Expression<Func<Entity, bool>> AddExtraPagedDataFilter(PagedDataSettings settings)
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
        protected virtual Expression<Func<Entity, bool>> AddPreConditionsPagedDataFilter(PagedDataSettings settings)
        {
            // Needs to be overriden by devs to add behavior to this.
            return null;
        }
    }
}
