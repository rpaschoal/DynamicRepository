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
    internal class MongoDBRepository<Key, Entity> : IRepository<Key, Entity> where Entity : class, new()
    {
        /// <summary>
        /// Favoring composition on paged data.
        /// </summary>
        private DataPager<Key, Entity> _dataPager;

        /// <summary>
        /// The mongoDB database instance to access the desired collection for this repository.
        /// </summary>
        private IMongoDatabase _mongoDatabase;

        /// <summary>
        /// Current MongoDB collection being used by this Repository instance.
        /// </summary>
        protected internal IMongoCollection<Entity> Collection;

        /// <summary>
        /// The name of the MongoDB collection where the data for this entity is stored.
        /// </summary>
        protected string CollectionName { get; }

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
        /// Default constructor of this Repository.
        /// </summary>
        /// <param name="mongoDatabase">
        /// The mongo database to be interfaced and fetch the data.
        /// </param>
        /// <param name="collectionName">The name of the MongoDB collection to be used with this repository.</param>
        public MongoDBRepository(IMongoDatabase mongoDatabase, string collectionName)
        {
            _mongoDatabase = mongoDatabase;
            CollectionName = collectionName;

            Collection = GetCollection<Entity>(collectionName);

            _dataPager = new DataPagerMongoDB<Key, Entity>();
        }

        /// <summary>
        /// Gets a collection by its name from the current <see cref="_mongoDatabase"/>. Useful for JOIN operations.
        /// </summary>
        /// <typeparam name="T">The type of the entity stored by the desired collection to be retrieved.</typeparam>
        /// <param name="collectionName">The name of the collection to be retrieved.</param>
        /// <returns>
        /// Instance of <see cref="IMongoCollection{TDocument}"/>.
        /// </returns>
        public IMongoCollection<T> GetCollection<T>(string collectionName) where T : class
        {
            return _mongoDatabase.GetCollection<T>(collectionName);
        }

        /// <summary>
        /// Gets underline Id filter for mongoDB.
        /// </summary>
        protected FilterDefinition<Entity> GetIdFilter(Entity entity)
        {
            // TODO: "Id" property name should be configurable
            return Builders<Entity>.Filter.Eq("Id", entity.GetType().GetProperty("Id").GetValue(entity, null));
        }

        /// <summary>
        /// Gets underline Id filter for mongoDB>
        /// </summary>
        /// <example>
        /// Implementation example:
        /// 
        /// return Builders<Entity>.Filter.Eq("_id", id);
        /// </example>
        protected FilterDefinition<Entity> GetIdFilter(Key id)
        {
            // TODO: "Id" property name should be configurable
            return Builders<Entity>.Filter.Eq("Id", id);
        }

        /// <summary>
        /// Gets an entity instance based on its <see cref="Key"/>.
        /// </summary>
        /// <param name="key">The desired entity key value.</param>
        /// <returns>Persisted entity if found, otherwise NULL.</returns>
        public Entity Get(Key id)
        {
            var queriedEntity = Collection.Find(GetIdFilter(id)).FirstOrDefault();

            return GlobalFilter != null ? new[] { queriedEntity }.AsQueryable().FirstOrDefault(GlobalFilter) : queriedEntity;
        }

        /// <summary>
        /// Gets an entity instance based on its <see cref="Key"/>.
        /// </summary>
        /// <param name="key">The desired entity key value.</param>
        /// <returns>Persisted entity if found, otherwise NULL.</returns>
        public async Task<Entity> GetAsync(Key id)
        {
            var queriedEntity = await (await Collection.FindAsync(GetIdFilter(id))).FirstOrDefaultAsync();

            return GlobalFilter != null ? new [] { queriedEntity }.AsQueryable().FirstOrDefault(GlobalFilter) : queriedEntity;
        }

        /// <summary>
        /// Persists a new entity model.
        /// </summary>
        /// <param name="entity">The new <see cref="Entity"/> instance to be persisted.</param>
        public void Insert(Entity entity)
        {
            Collection.InsertOne(entity);
        }

        /// <summary>
        /// Persists a new entity model.
        /// </summary>
        /// <param name="entity">The new <see cref="Entity"/> instance to be persisted.</param>
        public Task InsertAsync(Entity entity)
        {
            return Collection.InsertOneAsync(entity);
        }

        /// <summary>
        /// Updates an existing persisted entity.
        /// </summary>
        /// <param name="entityToUpdate">The <see cref="Entity"/> instance to be updated.</param>
        public void Update(Entity entityToUpdate)
        {
            Collection.ReplaceOne(GetIdFilter(entityToUpdate), entityToUpdate);
        }

        /// <summary>
        /// Updates an existing persisted entity.
        /// </summary>
        /// <param name="entityToUpdate">The <see cref="Entity"/> instance to be updated.</param>
        public Task UpdateAsync(Entity entityToUpdate)
        {
            return Collection.ReplaceOneAsync(GetIdFilter(entityToUpdate), entityToUpdate);
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="id">The primary key of the <see cref="Entity"/> to be deleted.</param>
        public void Delete(Key id)
        {
            Collection.DeleteOne(GetIdFilter(id));
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="id">The primary key of the <see cref="Entity"/> to be deleted.</param>
        public Task DeleteAsync(Key id)
        {
            return Collection.DeleteOneAsync(GetIdFilter(id));
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="entityToDelete">The <see cref="Entity"/> instance to be deleted.</param>
        public void Delete(Entity entityToDelete)
        {
            Collection.DeleteOne(GetIdFilter(entityToDelete));
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="entityToDelete">The <see cref="Entity"/> instance to be deleted.</param>
        public Task DeleteAsync(Entity entityToDelete)
        {
            return Collection.DeleteOneAsync(GetIdFilter(entityToDelete));
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
            return GlobalFilter != null ? Collection.AsQueryable().Where(GlobalFilter) : Collection.AsQueryable();
        }

        public IEnumerable<Entity> List(Expression<Func<Entity, bool>> filter = null, Func<IQueryable<Entity>, IOrderedQueryable<Entity>> orderBy = null, params string[] includeProperties)
        {
            IQueryable<Entity> query = GetQueryable();

            if (filter != null)
            {
                query = query.Where(filter);
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
        /// Returns a paged, filtered and sorted collection.
        /// </summary>
        /// <param name="settings">Settings model for the search.</param>
        /// <returns>Collection of filtered items result.</returns>
        public IPagedDataResult<Entity> GetPagedData(PagedDataSettings settings)
        {
            return _dataPager.GetPagedData(GetQueryable(), settings, AddPreConditionsPagedDataFilter(settings), AddExtraPagedDataFilter(settings));
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
