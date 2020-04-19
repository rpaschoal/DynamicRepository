using DynamicRepository.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicRepository.AddOn
{
    /// <summary>
    /// Proxy wrapper for <see cref="IRepository{Key, Entity}"/> so internals can be intialized during runtime.
    /// </summary>
    /// <typeparam name="Key">The key type of the entity being interfaced with through the current repository instance.</typeparam>
    /// <typeparam name="Entity">The entity type for the current repository instance.</typeparam>
    public class RepositoryProxy<Key, Entity> : IRepository<Key, Entity> where Entity : class
    {
        private IRepository<Key, Entity> _repositoryInternals;
        private IRepository<Key, Entity> RepositoryInternals { 
            get
            {
                if (_repositoryInternals == null)
                {
                    throw new NullReferenceException("Proxy internals were not initialized.");
                }

                return _repositoryInternals;
            } 
        }

        /// <summary>
        /// Proxies to be initialized just by DynamicRepository internals
        /// </summary>
        internal RepositoryProxy()
        {
        }

        internal void InitializeProxy(IRepository<Key, Entity> internals)
        {
            _repositoryInternals = internals;
        }

        public void HasGlobalFilter(Expression<Func<Entity, bool>> filter)
        {
            RepositoryInternals.HasGlobalFilter(filter);
        }

        /// <summary>
        /// Gets an entity instance based on its <see cref="Key"/>.
        /// </summary>
        /// <param name="key">The desired entity key value.</param>
        /// <returns>Persisted entity if found, otherwise NULL.</returns>
        public virtual Entity Get(Key key)
        {
            return RepositoryInternals.Get(key);
        }

        /// <summary>
        /// Gets an entity instance based on its <see cref="Key"/>.
        /// </summary>
        /// <param name="key">The desired entity key value.</param>
        /// <returns>Persisted entity if found, otherwise NULL.</returns>
        public virtual Task<Entity> GetAsync(Key key)
        {
            return RepositoryInternals.GetAsync(key);
        }

        /// <summary>
        /// Gets an entity instance based on its <see cref="Key"/>.
        /// </summary>
        /// <param name="key">The desired entity key value.</param>
        /// <param name="cancellationToken">A token used for cancelling propagation.</param>
        /// <returns>Persisted entity if found, otherwise NULL.</returns>
        public virtual Task<Entity> GetAsync(Key key, CancellationToken cancellationToken)
        {
            return RepositoryInternals.GetAsync(key, cancellationToken);
        }

        /// <summary>
        /// Persists a new entity model.
        /// </summary>
        /// <param name="entity">The new <see cref="Entity"/> instance to be persisted.</param>
        public virtual void Insert(Entity entity)
        {
            RepositoryInternals.Insert(entity);
        }

        /// <summary>
        /// Persists a new entity model.
        /// </summary>
        /// <param name="entity">The new <see cref="Entity"/> instance to be persisted.</param>
        public virtual Task InsertAsync(Entity entity)
        {
            return RepositoryInternals.InsertAsync(entity);
        }

        /// <summary>
        /// Persists a new entity model.
        /// </summary>
        /// <param name="entity">The new <see cref="Entity"/> instance to be persisted.</param>
        /// <param name="cancellationToken">A token used for cancelling propagation.</param>
        public virtual Task InsertAsync(Entity entity, CancellationToken cancellationToken)
        {
            return RepositoryInternals.InsertAsync(entity, cancellationToken);
        }

        /// <summary>
        /// Updates an existing persisted entity.
        /// </summary>
        /// <param name="entityToUpdate">The <see cref="Entity"/> instance to be updated.</param>
        public virtual void Update(Entity entityToUpdate)
        {
            RepositoryInternals.Update(entityToUpdate);
        }

        /// <summary>
        /// Updates an existing persisted entity.
        /// </summary>
        /// <param name="entityToUpdate">The <see cref="Entity"/> instance to be updated.</param>
        public virtual Task UpdateAsync(Entity entityToUpdate)
        {
            return RepositoryInternals.UpdateAsync(entityToUpdate);
        }

        /// <summary>
        /// Updates an existing persisted entity.
        /// </summary>
        /// <param name="entityToUpdate">The <see cref="Entity"/> instance to be updated.</param>
        /// <param name="cancellationToken">A token used for cancelling propagation.</param>
        public virtual Task UpdateAsync(Entity entityToUpdate, CancellationToken cancellationToken)
        {
            return RepositoryInternals.UpdateAsync(entityToUpdate, cancellationToken);
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="id">The primary key of the <see cref="Entity"/> to be deleted.</param>
        public virtual void Delete(Key id)
        {
            RepositoryInternals.Delete(id);
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="id">The primary key of the <see cref="Entity"/> to be deleted.</param>
        public virtual Task DeleteAsync(Key id)
        {
            return RepositoryInternals.DeleteAsync(id);
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="id">The primary key of the <see cref="Entity"/> to be deleted.</param>
        /// <param name="cancellationToken">A token used for cancelling propagation.</param>
        public virtual Task DeleteAsync(Key id, CancellationToken cancellationToken)
        {
            return RepositoryInternals.DeleteAsync(id, cancellationToken);
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="entityToDelete">The <see cref="Entity"/> instance to be deleted.</param>
        public void Delete(Entity entityToDelete)
        {
            RepositoryInternals.Delete(entityToDelete);
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="entityToDelete">The <see cref="Entity"/> instance to be deleted.</param>
        public Task DeleteAsync(Entity entityToDelete)
        {
            return RepositoryInternals.DeleteAsync(entityToDelete);
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="entityToDelete">The <see cref="Entity"/> instance to be deleted.</param>
        /// <param name="cancellationToken">A token used for cancelling propagation.</param>
        public Task DeleteAsync(Entity entityToDelete, CancellationToken cancellationToken)
        {
            return RepositoryInternals.DeleteAsync(entityToDelete, cancellationToken);
        }

        /// <summary>
        /// Returns all entries of this entity.
        /// </summary>
        public IEnumerable<Entity> ListAll()
        {
            return RepositoryInternals.ListAll();
        }

        /// <summary>
        /// Gets a queryable instance of the current data set.
        /// </summary>
        public IQueryable<Entity> GetQueryable()
        {
            return RepositoryInternals.GetQueryable();
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
            return RepositoryInternals.List(filter, orderBy, includeProperties);
        }

        /// <summary>
        /// Returns a collection of data results that can be paged.
        /// </summary>
        /// <param name="settings">Settings for the search.</param>
        /// <returns>Filled PagedData instance.</returns>
        public IPagedDataResult<Entity> GetPagedData(PagedDataSettings settings)
        {
            return RepositoryInternals.GetPagedData(settings);
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
