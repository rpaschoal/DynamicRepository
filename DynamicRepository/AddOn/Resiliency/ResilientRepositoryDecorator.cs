using DynamicRepository.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Polly;
using System.Threading;

namespace DynamicRepository.AddOn.Resiliency
{
    /// <summary>
    /// Adds resiliency to concrete implementations of <see cref="IRepository{Key, Entity}"/> by using Polly and retry mechanisms.
    /// </summary>
    /// <typeparam name="Key">The key type of the entity being interfaced with through the current repository instance.</typeparam>
    /// <typeparam name="Entity">The entity type for the current repository instance.</typeparam>
    /// <typeparam name="ExceptionType">The exception type that is going to be caught by the retry mechanism.</typeparam>
    internal sealed class ResilientRepositoryDecorator<Key, Entity, ExceptionType> : IRepository<Key, Entity>
        where Entity : class
        where ExceptionType : Exception
    {
        private const int NUMBER_OF_RETRIES = 2;
        private readonly IRepository<Key, Entity> _repository;
        private readonly Policy _resiliencySyncPolicy;
        private readonly AsyncPolicy _resiliencyAsyncPolicy;

        private Policy SyncRetryPolicyFactory => Policy
            .Handle<ExceptionType>()
            .WaitAndRetry(NUMBER_OF_RETRIES, retryAttempt => TimeSpan.FromMilliseconds(500));

        private AsyncPolicy ASyncRetryPolicyFactory => Policy
            .Handle<ExceptionType>()
            .WaitAndRetryAsync(NUMBER_OF_RETRIES, retryAttempt => TimeSpan.FromMilliseconds(500));

        internal ResilientRepositoryDecorator(IRepository<Key, Entity> repository)
        {
            _repository = repository;
            _resiliencySyncPolicy = SyncRetryPolicyFactory;
            _resiliencyAsyncPolicy = ASyncRetryPolicyFactory;
        }

        public IQueryable<Entity> GetQueryable()
        {
            return _repository.GetQueryable();
        }

        public void HasGlobalFilter(Expression<Func<Entity, bool>> filter)
        {
            _repository.HasGlobalFilter(filter);
        }

        public void Delete(Key id)
        {
            _resiliencySyncPolicy.Execute(() => _repository.Delete(id));
        }

        public void Delete(Entity entityToDelete)
        {
            _resiliencySyncPolicy.Execute(() => _repository.Delete(entityToDelete));
        }

        public Task DeleteAsync(Key id)
        {
            return DeleteAsync(id, CancellationToken.None);
        }

        public Task DeleteAsync(Key id, CancellationToken cancellationToken)
        {
            return _resiliencyAsyncPolicy.ExecuteAsync(async () => await _repository.DeleteAsync(id, cancellationToken));
        }

        public Task DeleteAsync(Entity entityToDelete)
        {
            return DeleteAsync(entityToDelete, CancellationToken.None);
        }

        public Task DeleteAsync(Entity entityToDelete, CancellationToken cancellationToken)
        {
            return _resiliencyAsyncPolicy.ExecuteAsync(async () => await _repository.DeleteAsync(entityToDelete, cancellationToken));
        }

        public Entity Get(Key id)
        {
            return _resiliencySyncPolicy.Execute(() => _repository.Get(id));
        }

        public Task<Entity> GetAsync(Key id)
        {
            return GetAsync(id, CancellationToken.None);
        }

        public Task<Entity> GetAsync(Key id, CancellationToken cancellationToken)
        {
            return _resiliencyAsyncPolicy.ExecuteAsync(async () => await _repository.GetAsync(id, cancellationToken));
        }

        public IPagedDataResult<Entity> GetPagedData(PagedDataSettings settings)
        {
            return _resiliencySyncPolicy.Execute(() => _repository.GetPagedData(settings));
        }

        public void Insert(Entity entity)
        {
            _resiliencySyncPolicy.Execute(() => _repository.Insert(entity));
        }

        public Task InsertAsync(Entity entity)
        {
            return InsertAsync(entity, CancellationToken.None);
        }

        public Task InsertAsync(Entity entity, CancellationToken cancellationToken)
        {
            return _resiliencyAsyncPolicy.ExecuteAsync(async () => await _repository.InsertAsync(entity, cancellationToken));
        }

        public IEnumerable<Entity> List(Expression<Func<Entity, bool>> filter = null, Func<IQueryable<Entity>, IOrderedQueryable<Entity>> orderBy = null, params string[] includeProperties)
        {
            return _resiliencySyncPolicy.Execute(() => _repository.List(filter, orderBy, includeProperties));
        }

        public IEnumerable<Entity> ListAll()
        {
            return _resiliencySyncPolicy.Execute(() => _repository.ListAll());
        }

        public void Update(Entity entityToUpdate)
        {
            _resiliencySyncPolicy.Execute(() => _repository.Update(entityToUpdate));
        }

        public Task UpdateAsync(Entity entityToUpdate)
        {
            return UpdateAsync(entityToUpdate, CancellationToken.None);
        }

        public Task UpdateAsync(Entity entityToUpdate, CancellationToken cancellationToken)
        {
            return _resiliencyAsyncPolicy.ExecuteAsync(async () => await _repository.UpdateAsync(entityToUpdate, cancellationToken));
        }
    }
}
