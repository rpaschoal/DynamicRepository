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
        public Repository(IMongoDatabase mongoDatabase, string collectionName) : base(BuildInternals(mongoDatabase, collectionName, out var repositoryInternals))
        {
            _mongoDBRepository = repositoryInternals;
        }

        private static IRepository<Key, Entity> BuildInternals(IMongoDatabase mongoDatabase, string collectionName, out MongoDBRepository<Key, Entity> repositoryInternals)
        {
            repositoryInternals = new MongoDBRepository<Key, Entity>(mongoDatabase, collectionName);

            return new RepositoryAddOnBuilder<Key, Entity>(repositoryInternals)
                .AddResiliency()
                .Build();
        }
    }
}
