using DynamicRepository.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Polly;

namespace DynamicRepository.Resiliency
{
    public sealed class ResilientRepositoryDecorator<Key, Entity> : IRepository<Key, Entity> where Entity : class, new()
    {
        private const int NUMBER_OF_RETRIES = 3;
        private readonly IRepository<Key, Entity> _repository;
        private readonly Policy _resiliencySyncPolicy;
        private readonly AsyncPolicy _resiliencyAsyncPolicy;

        private Policy SyncRetryPolicyFactory => Policy
                                                    .Handle<Exception>()
                                                    .WaitAndRetry(NUMBER_OF_RETRIES, retryAttempt =>
                                                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                                                    );

        private AsyncPolicy ASyncRetryPolicyFactory => Policy
                                                    .Handle<Exception> ()
                                                    .WaitAndRetryAsync(NUMBER_OF_RETRIES, retryAttempt =>
                                                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                                                    );

        public ResilientRepositoryDecorator(IRepository<Key, Entity> repository)
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
            return _resiliencyAsyncPolicy.ExecuteAsync(async () => await _repository.DeleteAsync(id));
        }

        public Task DeleteAsync(Entity entityToDelete)
        {
            return _resiliencyAsyncPolicy.ExecuteAsync(async () => await _repository.DeleteAsync(entityToDelete));
        }

        public Entity Get(Key id)
        {
            return _resiliencySyncPolicy.Execute(() => _repository.Get(id));
        }

        public Task<Entity> GetAsync(Key id)
        {
            return _resiliencyAsyncPolicy.ExecuteAsync(async () => await _repository.GetAsync(id));
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
            return _resiliencyAsyncPolicy.ExecuteAsync(async () => await _repository.InsertAsync(entity));
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
            return _resiliencyAsyncPolicy.ExecuteAsync(async () => await _repository.UpdateAsync(entityToUpdate));
        }
    }
}
