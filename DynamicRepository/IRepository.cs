using DynamicRepository.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("DynamicRepository.EF")]
[assembly: InternalsVisibleTo("DynamicRepository.EFCore")]
[assembly: InternalsVisibleTo("DynamicRepository.MongoDB")]
[assembly: InternalsVisibleTo("DynamicRepository.Tests")]
namespace DynamicRepository
{
    /// <summary>
    /// Default contract for repository pattern general data access methods.
    /// </summary>
    /// <typeparam name="Key">The key type of current entity type. For composed primary keys use a new class type definition or an <see cref="object[]"/> array.</typeparam>
    /// <typeparam name="Entity">The type of the entity being persisted or retrieved.</typeparam>
    public interface IRepository<Key, Entity> where Entity : class
    {
        /// <summary>
        /// Adds a global filter expression to all operations which query for data.
        /// </summary>
        /// <remarks>This method was inspired by "HasQueryFilter" found on EF Core.</remarks>
        void HasGlobalFilter(Expression<Func<Entity, bool>> filter);

        /// <summary>
        /// Gets an entity instance based on its <see cref="Key"/>.
        /// </summary>
        /// <param name="key">The desired entity key value.</param>
        /// <returns>Persisted entity if found, otherwise NULL.</returns>
        Entity Get(Key id);

        /// <summary>
        /// Gets an entity instance based on its <see cref="Key"/>.
        /// </summary>
        /// <param name="key">The desired entity key value.</param>
        /// <returns>Persisted entity if found, otherwise NULL.</returns>
        Task<Entity> GetAsync(Key id);

        /// <summary>
        /// Gets an entity instance based on its <see cref="Key"/>.
        /// </summary>
        /// <param name="key">The desired entity key value.</param>
        /// <param name="cancellationToken">A token used for cancelling propagation.</param>
        /// <returns>Persisted entity if found, otherwise NULL.</returns>
        Task<Entity> GetAsync(Key id, CancellationToken cancellationToken);

        /// <summary>
        /// Persists a new entity model.
        /// </summary>
        /// <param name="entity">The new <see cref="Entity"/> instance to be persisted.</param>
        void Insert(Entity entity);

        /// <summary>
        /// Persists a new entity model.
        /// </summary>
        /// <param name="entity">The new <see cref="Entity"/> instance to be persisted.</param>
        Task InsertAsync(Entity entity);

        /// <summary>
        /// Persists a new entity model.
        /// </summary>
        /// <param name="entity">The new <see cref="Entity"/> instance to be persisted.</param>
        /// <param name="cancellationToken">A token used for cancelling propagation.</param>
        Task InsertAsync(Entity entity, CancellationToken cancellationToken);

        /// <summary>
        /// Updates an existing persisted entity.
        /// </summary>
        /// <param name="entityToUpdate">The <see cref="Entity"/> instance to be updated.</param>
        void Update(Entity entityToUpdate);

        /// <summary>
        /// Updates an existing persisted entity.
        /// </summary>
        /// <param name="entityToUpdate">The <see cref="Entity"/> instance to be updated.</param>
        Task UpdateAsync(Entity entityToUpdate);

        /// <summary>
        /// Updates an existing persisted entity.
        /// </summary>
        /// <param name="entityToUpdate">The <see cref="Entity"/> instance to be updated.</param>
        /// <param name="cancellationToken">A token used for cancelling propagation.</param>
        Task UpdateAsync(Entity entityToUpdate, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="id">The primary key of the <see cref="Entity"/> to be deleted.</param>
        void Delete(Key id);

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="id">The primary key of the <see cref="Entity"/> to be deleted.</param>
        Task DeleteAsync(Key id);

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="id">The primary key of the <see cref="Entity"/> to be deleted.</param>
        /// <param name="cancellationToken">A token used for cancelling propagation.</param>
        Task DeleteAsync(Key id, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="entityToDelete">The <see cref="Entity"/> instance to be deleted.</param>
        void Delete(Entity entityToDelete);

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="entityToDelete">The <see cref="Entity"/> instance to be deleted.</param>
        Task DeleteAsync(Entity entityToDelete);

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="entityToDelete">The <see cref="Entity"/> instance to be deleted.</param>
        /// <param name="cancellationToken">A token used for cancelling propagation.</param>
        Task DeleteAsync(Entity entityToDelete, CancellationToken cancellationToken);

        /// <summary>
        /// Returns all entries of this entity.
        /// </summary>
        IEnumerable<Entity> ListAll();

        /// <summary>
        /// Gets a queryable instance of the current data set.
        /// </summary>
        IQueryable<Entity> GetQueryable();

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
        IEnumerable<Entity> List(
            Expression<Func<Entity, bool>> filter = null,
            Func<IQueryable<Entity>, IOrderedQueryable<Entity>> orderBy = null,
            params string[] includeProperties);

        /// <summary>
        /// Returns a paged, filtered and sorted collection.
        /// </summary>
        /// <param name="settings">Settings model for the search.</param>
        /// <returns>Collection of filtered items result.</returns>
        IPagedDataResult<Entity> GetPagedData(PagedDataSettings settings);
    }
}
