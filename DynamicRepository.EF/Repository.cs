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
using System.Threading.Tasks;

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
        /// Global filter instance set by <see cref="HasGlobalFilter(Expression{Func{Entity, bool}})" />
        /// </summary>
        private Expression<Func<Entity, bool>> GlobalFilter { get; set; }

        /// <summary>
        /// Adds a global filter expression to all operations which query for data.
        /// </summary>
        /// <remarks>This method was inspired by "HasQueryFilter" found on EF Core.</remarks>
        public void HasGlobalFilter(Expression<Func<Entity, bool>> filter)
        {
            GlobalFilter = filter;
        }

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
        /// Gets an entity instance based on its <see cref="Key"/>.
        /// </summary>
        /// <param name="key">The desired entity key value.</param>
        /// <returns>Persisted entity if found, otherwise NULL.</returns>
        public virtual Entity Get(Key key)
        {
            Entity queriedEntity;

            if (key is Array)
            {
                // This is to handle entity framework find by composite key
                queriedEntity = DbSet.Find((key as IEnumerable).Cast<object>().ToArray());
            }
            else
            {
                queriedEntity = DbSet.Find(key);
            }

            return GlobalFilter != null ? new[] { queriedEntity }.AsQueryable().FirstOrDefault(GlobalFilter) : queriedEntity;
        }

        /// <summary>
        /// Gets an entity instance based on its <see cref="Key"/>.
        /// </summary>
        /// <param name="key">The desired entity key value.</param>
        /// <returns>Persisted entity if found, otherwise NULL.</returns>
        public virtual async Task<Entity> GetAsync(Key key)
        {
            Entity queriedEntity;

            if (key is Array)
            {
                // This is to handle entity framework find by composite key
                queriedEntity = await DbSet.FindAsync((key as IEnumerable).Cast<object>().ToArray());
            }
            else
            {
                queriedEntity = await DbSet.FindAsync(key);
            }

            return GlobalFilter != null ? await new[] { queriedEntity }.AsQueryable().FirstOrDefaultAsync(GlobalFilter) :  queriedEntity;
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
        /// Persists a new entity model.
        /// </summary>
        /// <param name="entity">The new <see cref="Entity"/> instance to be persisted.</param>
        public virtual Task InsertAsync(Entity entity)
        {
            return Task.Run(() => DbSet.Add(entity));
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
        /// Updates an existing persisted entity.
        /// </summary>
        /// <param name="entityToUpdate">The <see cref="Entity"/> instance to be updated.</param>
        public virtual Task UpdateAsync(Entity entityToUpdate)
        {
            return Task.Run(() => Context.Entry(entityToUpdate).State = EntityState.Modified);
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="id">The primary key of the <see cref="Entity"/> to be deleted.</param>
        public virtual void Delete(Key id)
        {
            Delete(Get(id));
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="id">The primary key of the <see cref="Entity"/> to be deleted.</param>
        public virtual Task DeleteAsync(Key id)
        {
            return Task.Run(async () => DeleteAsync(await GetAsync(id)));
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
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="entityToDelete">The <see cref="Entity"/> instance to be deleted.</param>
        public Task DeleteAsync(Entity entityToDelete)
        {
            if (entityToDelete != null)
            {
                return Task.Run(() => DbSet.Remove(entityToDelete));
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns all entries of this entity.
        /// </summary>
        public IEnumerable<Entity> ListAll()
        {
            return GetQueryable();
        }

        /// <summary>
        /// Gets a queryable instance of the current data set.
        /// </summary>
        public IQueryable<Entity> GetQueryable()
        {
            return DbSet.AsQueryable().Where(GlobalFilter);
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
            IQueryable<Entity> query = GetQueryable();

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
        /// Returns a collection of data results that can be paged.
        /// </summary>
        /// <param name="settings">Settings for the search.</param>
        /// <returns>Filled PagedData instance.</returns>
        public IPagedDataResult<Entity> GetPagedData(PagedDataSettings settings)
        {
            return _dataSourcePager.GetPagedData(GetQueryable(), settings, AddPreConditionsPagedDataFilter(settings), AddExtraPagedDataFilter(settings));
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
