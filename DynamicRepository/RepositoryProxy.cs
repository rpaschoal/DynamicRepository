using DynamicRepository.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DynamicRepository
{
    public class RepositoryProxy<Key, Entity> : IRepository<Key, Entity> where Entity : class
    {
        private IRepository<Key, Entity> _repositoryInternals;

        public RepositoryProxy(IRepository<Key, Entity> internals)
        {
            _repositoryInternals = internals;
        }

        public void HasGlobalFilter(Expression<Func<Entity, bool>> filter)
        {
            _repositoryInternals.HasGlobalFilter(filter);
        }

        /// <summary>
        /// Gets an entity instance based on its <see cref="Key"/>.
        /// </summary>
        /// <param name="key">The desired entity key value.</param>
        /// <returns>Persisted entity if found, otherwise NULL.</returns>
        public virtual Entity Get(Key key)
        {
            return _repositoryInternals.Get(key);
        }

        /// <summary>
        /// Gets an entity instance based on its <see cref="Key"/>.
        /// </summary>
        /// <param name="key">The desired entity key value.</param>
        /// <returns>Persisted entity if found, otherwise NULL.</returns>
        public virtual Task<Entity> GetAsync(Key key)
        {
            return _repositoryInternals.GetAsync(key);
        }

        /// <summary>
        /// Persists a new entity model.
        /// </summary>
        /// <param name="entity">The new <see cref="Entity"/> instance to be persisted.</param>
        public virtual void Insert(Entity entity)
        {
            _repositoryInternals.Insert(entity);
        }

        /// <summary>
        /// Persists a new entity model.
        /// </summary>
        /// <param name="entity">The new <see cref="Entity"/> instance to be persisted.</param>
        public virtual Task InsertAsync(Entity entity)
        {
            return _repositoryInternals.InsertAsync(entity);
        }

        /// <summary>
        /// Updates an existing persisted entity.
        /// </summary>
        /// <param name="entityToUpdate">The <see cref="Entity"/> instance to be updated.</param>
        public virtual void Update(Entity entityToUpdate)
        {
            _repositoryInternals.Update(entityToUpdate);
        }

        /// <summary>
        /// Updates an existing persisted entity.
        /// </summary>
        /// <param name="entityToUpdate">The <see cref="Entity"/> instance to be updated.</param>
        public virtual Task UpdateAsync(Entity entityToUpdate)
        {
            return _repositoryInternals.UpdateAsync(entityToUpdate);
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="id">The primary key of the <see cref="Entity"/> to be deleted.</param>
        public virtual void Delete(Key id)
        {
            _repositoryInternals.Delete(id);
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="id">The primary key of the <see cref="Entity"/> to be deleted.</param>
        public virtual Task DeleteAsync(Key id)
        {
            return _repositoryInternals.DeleteAsync(id);
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="entityToDelete">The <see cref="Entity"/> instance to be deleted.</param>
        public void Delete(Entity entityToDelete)
        {
            _repositoryInternals.Delete(entityToDelete);
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="entityToDelete">The <see cref="Entity"/> instance to be deleted.</param>
        public Task DeleteAsync(Entity entityToDelete)
        {
            return _repositoryInternals.DeleteAsync(entityToDelete);
        }

        /// <summary>
        /// Returns all entries of this entity.
        /// </summary>
        public IEnumerable<Entity> ListAll()
        {
            return _repositoryInternals.ListAll();
        }

        /// <summary>
        /// Gets a queryable instance of the current data set.
        /// </summary>
        public IQueryable<Entity> GetQueryable()
        {
            return _repositoryInternals.GetQueryable();
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
            return _repositoryInternals.List(filter, orderBy, includeProperties);
        }

        /// <summary>
        /// Returns a collection of data results that can be paged.
        /// </summary>
        /// <param name="settings">Settings for the search.</param>
        /// <returns>Filled PagedData instance.</returns>
        public IPagedDataResult<Entity> GetPagedData(PagedDataSettings settings)
        {
            return _repositoryInternals.GetPagedData(settings);
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
