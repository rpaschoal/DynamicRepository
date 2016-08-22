using DynamicRepository.Filter;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DynamicRepository.MongoDB
{
    /// <summary>
    /// MongoDB implementation of Dynamic Repository data access.
    /// </summary>
    /// <typeparam name="Key">
    /// The entity type of the key of this collection.
    /// </typeparam>
    /// <typeparam name="Entity">
    /// The Entity type mapped by the desired collection.
    /// </typeparam>
    public abstract class Repository<Key, Entity> : IRepository<Key, Entity> where Entity : class, new()
    {
        /// <summary>
        /// Favoring composition on paged data.
        /// </summary>
        private DataPager<Key, Entity> _dataPager;

        /// <summary>
        /// The mongoDB database instance to access the desired collection for this repository.
        /// </summary>
        IMongoDatabase _mongoDatabase;

        /// <summary>
        /// Current MongoDB collection being used by this Repository instance.
        /// </summary>
        private IMongoCollection<Entity> _collection;

        /// <summary>
        /// The name of the MongoDB collection where the data for this entity is stored.
        /// </summary>
        protected abstract string CollectionName { get; }

        /// <summary>
        /// Default constructor of this Repository.
        /// </summary>
        /// <param name="mongoDatabase">
        /// The mongo database to be interfaced and fetch the data.
        /// </param>
        public Repository(IMongoDatabase mongoDatabase)
        {
            _mongoDatabase = mongoDatabase;
            _collection = _mongoDatabase.GetCollection<Entity>(this.CollectionName);

            _dataPager = new DataPagerMongoDB<Key, Entity>();
        }

        /// <summary>
        /// Gets underline Id filter for mongoDB.
        /// </summary>
        protected abstract FilterDefinition<Entity> GetIdFilter(Entity entity);

        /// <summary>
        /// Gets underline Id filter for mongoDB>
        /// </summary>
        /// <example>
        /// Implementation example:
        /// 
        /// return Builders<Entity>.Filter.Eq("_id", id);
        /// </example>
        protected abstract FilterDefinition<Entity> GetIdFilter(Key id);

        /// <summary>
        /// Gets an entity instance based on its <see cref="Key"/>.
        /// </summary>
        /// <param name="key">The desired entity key value.</param>
        /// <returns>Persisted entity if found, otherwise NULL.</returns>
        public Entity Get(Key id)
        {
            return (_collection.Find(GetIdFilter(id))).FirstOrDefault();
        }

        /// <summary>
        /// Persists a new entity model.
        /// </summary>
        /// <param name="entity">The new <see cref="Entity"/> instance to be persisted.</param>
        public void Insert(Entity entity)
        {
            _collection.InsertOne(entity);
        }

        /// <summary>
        /// Updates an existing persisted entity.
        /// </summary>
        /// <param name="entityToUpdate">The <see cref="Entity"/> instance to be updated.</param>
        public void Update(Entity entityToUpdate)
        {
            _collection.ReplaceOne(GetIdFilter(entityToUpdate), entityToUpdate);
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="id">The primary key of the <see cref="Entity"/> to be deleted.</param>
        public void Delete(Key id)
        {
            _collection.DeleteOne(GetIdFilter(id));
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="entityToDelete">The <see cref="Entity"/> instance to be deleted.</param>
        public void Delete(Entity entityToDelete)
        {
            _collection.DeleteOne(GetIdFilter(entityToDelete));
        }

        public IEnumerable<Entity> List(Expression<Func<Entity, bool>> filter = null, Func<IQueryable<Entity>, IOrderedQueryable<Entity>> orderBy = null, params string[] includeProperties)
        {
            // TODO: Have a look on this later...
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a paged, filtered and sorted collection.
        /// </summary>
        /// <param name="settings">Settings model for the search.</param>
        /// <returns>Collection of filtered items result.</returns>
        public IPagedDataResult<Entity> GetPagedData(PagedDataSettings settings)
        {
            return _dataPager.GetPagedData(_collection.AsQueryable(), settings, this.AddPreConditionsPagedDataFilter(settings), this.AddExtraPagedDataFilter(settings));
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
