using DynamicRepository.AddOn;
using MongoDB.Driver;

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
    public abstract class Repository<Key, Entity> : RepositoryProxy<Key, Entity>, IRepository<Key, Entity> where Entity : class, new()
    {
        /// <summary>
        /// Repository internals with data access methods for MongoDB.
        /// </summary>
        private readonly MongoDBRepository<Key, Entity> _mongoDBRepository;

        /// <summary>
        /// Current MongoDB collection being used by this Repository instance.
        /// </summary>
        protected IMongoCollection<Entity> Collection
        {
            get
            {
                return _mongoDBRepository.Collection;
            }
        }

        /// <summary>
        /// Default constructor of this Repository.
        /// </summary>
        /// <param name="mongoDatabase">
        /// The mongo database to be interfaced and fetch the data.
        /// </param>
        /// <param name="collectionName">The collection name to be accessed by this repository.</param>
        /// <param name="idPropertyName">Name of the Id property for each document of the collection being accessed. Default is "Id".</param>
        public Repository(IMongoDatabase mongoDatabase, string collectionName, string idPropertyName = "Id")
        {
            _mongoDBRepository = new MongoDBRepository<Key, Entity>(mongoDatabase,
                collectionName,
                idPropertyName,
                AddPreConditionsPagedDataFilter,
                AddExtraPagedDataFilter);

            var builtRepository = new RepositoryAddOnBuilder<Key, Entity>(_mongoDBRepository)
                .AddResiliency<MongoException>()
                .Build();

            InitializeProxy(builtRepository);
        }
    }
}
