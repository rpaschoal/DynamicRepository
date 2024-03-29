﻿using DynamicRepository.Filter;
using DynamicRepository.MongoDB.Transaction;
using DynamicRepository.Transaction;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

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
    internal class MongoDBRepository<Key, Entity> : ITransactionRegister, IRepository<Key, Entity> where Entity : class, new()
    {
        /// <summary>
        /// Favoring composition on paged data.
        /// </summary>
        private readonly DataPager<Key, Entity> _dataPager;

        /// <summary>
        /// The mongoDB database instance to access the desired collection for this repository.
        /// </summary>
        private readonly IMongoDatabase _mongoDatabase;

        /// <summary>
        /// Name of the Id property for each document of the collection being accessed.
        /// </summary>
        private readonly string _idPropertyName;

        /// <summary>
        /// Delegate supplied via constructor for pre-condition filtering.
        /// </summary>
        private readonly Func<PagedDataSettings, Expression<Func<Entity, bool>>> _preConditionsToPagedDataFilterDelegate;

        /// <summary>
        /// Delegate supplied via constructor for extra paged data filtering.
        /// </summary>
        private readonly Func<PagedDataSettings, Expression<Func<Entity, bool>>> _extraPagedDataFilterDelegate;

        /// <summary>
        /// Current MongoDB collection being used by this Repository instance.
        /// </summary>
        protected internal IMongoCollection<Entity> Collection;

        /// <summary>
        /// The name of the MongoDB collection where the data for this entity is stored.
        /// </summary>
        protected string CollectionName { get; }

        private readonly object _transactionLock = new object();
        private MongoDBTransaction _transactionInstance;
        private MongoDBTransaction Transaction
        {
            get
            {
                lock (_transactionLock)
                {
                    if (_transactionInstance != null && _transactionInstance.HasBeenDisposed)
                    {
                        _transactionInstance = null;
                    }

                    return _transactionInstance;
                }
            }
            set
            {
                lock (_transactionLock)
                {
                    _transactionInstance = value;
                }
            }
        }

        /// <summary>
        /// Global filter instance set by <see cref="HasGlobalFilter(Expression{Func{Entity, bool}})" />
        /// </summary>
        private Expression<Func<Entity, bool>> GlobalFilter { get; set; }

        /// <summary>
        /// Default constructor of this Repository.
        /// </summary>
        /// <param name="mongoDatabase">
        /// The mongo database to be interfaced and fetch the data.
        /// </param>
        /// <param name="collectionName">The name of the MongoDB collection to be used with this repository.</param>
        public MongoDBRepository(IMongoDatabase mongoDatabase, 
            string collectionName,
            string idPropertyName,
            Func<PagedDataSettings, Expression<Func<Entity, bool>>> preConditionsToPagedDataFilterDelegate,
            Func<PagedDataSettings, Expression<Func<Entity, bool>>> extraPagedDataFilterDelegate)
        {
            _mongoDatabase = mongoDatabase;
            CollectionName = collectionName;
            _idPropertyName = idPropertyName;
            _preConditionsToPagedDataFilterDelegate = preConditionsToPagedDataFilterDelegate;
            _extraPagedDataFilterDelegate = extraPagedDataFilterDelegate;

            Collection = GetCollection<Entity>(collectionName);

            _dataPager = new DataPagerMongoDB<Key, Entity>();
        }

        /// <summary>
        /// Adds a global filter expression to all operations which query for data.
        /// </summary>
        /// <remarks>This method was inspired by "HasQueryFilter" found on EF Core.</remarks>
        public void HasGlobalFilter(Expression<Func<Entity, bool>> filter)
        {
            GlobalFilter = filter;
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
            return Builders<Entity>.Filter.Eq(_idPropertyName, entity.GetType().GetProperty("Id").GetValue(entity, null));
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
            return Builders<Entity>.Filter.Eq(_idPropertyName, id);
        }

        public TransactionScope StartTransactionScope() => new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        public ITransaction StartTransaction()
        {
            Transaction = new MongoDBTransaction(_mongoDatabase.Client);

            return Transaction;
        }

        public void RegisterTransaction(ITransaction transaction)
        {
            Transaction = transaction as MongoDBTransaction;
        }

        private void EnlistWithCurrentTransactionScope()
        {
            if (System.Transactions.Transaction.Current != null)
            {
                var ambientTransactionId = System.Transactions.Transaction.Current.TransactionInformation.LocalIdentifier;

                if (AmbientTransactionRegister.AmbientTransactions.ContainsKey(ambientTransactionId))
                {
                    RegisterTransaction(AmbientTransactionRegister.AmbientTransactions[ambientTransactionId]);
                }
                else
                {
                    StartTransaction();

                    AmbientTransactionRegister.AmbientTransactions.TryAdd(ambientTransactionId, Transaction);

                    System.Transactions.Transaction.Current.TransactionCompleted += (sender, e) => {
                        AmbientTransactionRegister.AmbientTransactions.TryRemove(ambientTransactionId, out _);
                        Transaction = null;
                    };

                    var enlistment = new MongoDBTransactionScopeEnlistment(Transaction);
                    System.Transactions.Transaction.Current.EnlistVolatile(enlistment, System.Transactions.EnlistmentOptions.None);
                }
            }
        }

        /// <summary>
        /// Gets an entity instance based on its <see cref="Key"/>.
        /// </summary>
        /// <param name="key">The desired entity key value.</param>
        /// <returns>Persisted entity if found, otherwise NULL.</returns>
        public Entity Get(Key id)
        {
            EnlistWithCurrentTransactionScope();

            Entity queriedEntity;
            if (Transaction != null)
            {
                queriedEntity = Collection
                    .Find(Transaction.Session, GetIdFilter(id))
                    .FirstOrDefault();
            }
            else
            {
                queriedEntity = Collection.Find(GetIdFilter(id)).FirstOrDefault();
            }

            return GlobalFilter != null && queriedEntity != null ? new[] { queriedEntity }.AsQueryable().FirstOrDefault(GlobalFilter) : queriedEntity;
        }

        /// <summary>
        /// Gets an entity instance based on its <see cref="Key"/>.
        /// </summary>
        /// <param name="key">The desired entity key value.</param>
        /// <returns>Persisted entity if found, otherwise NULL.</returns>
        public Task<Entity> GetAsync(Key id)
        {
            return GetAsync(id, CancellationToken.None);
        }

        /// <summary>
        /// Gets an entity instance based on its <see cref="Key"/>.
        /// </summary>
        /// <param name="key">The desired entity key value.</param>
        /// <param name="cancellationToken">A token used for cancelling propagation.</param>
        /// <returns>Persisted entity if found, otherwise NULL.</returns>
        public async Task<Entity> GetAsync(Key id, CancellationToken cancellationToken)
        {
            EnlistWithCurrentTransactionScope();

            Entity queriedEntity;
            if (Transaction != null)
            {
                queriedEntity = await (await Collection.FindAsync(Transaction.Session, GetIdFilter(id), cancellationToken: cancellationToken).ConfigureAwait(false))
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
            }
            else
            {
                queriedEntity = await (await Collection.FindAsync(GetIdFilter(id), cancellationToken: cancellationToken).ConfigureAwait(false))
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
            }

            return GlobalFilter != null && queriedEntity != null ? new[] { queriedEntity }.AsQueryable().FirstOrDefault(GlobalFilter) : queriedEntity;
        }

        /// <summary>
        /// Persists a new entity model.
        /// </summary>
        /// <param name="entity">The new <see cref="Entity"/> instance to be persisted.</param>
        public void Insert(Entity entity)
        {
            EnlistWithCurrentTransactionScope();

            if (Transaction != null)
            {
                Collection.InsertOne(Transaction.Session, entity);
            }
            else
            {
                Collection.InsertOne(entity);
            }
        }

        /// <summary>
        /// Persists a new entity model.
        /// </summary>
        /// <param name="entity">The new <see cref="Entity"/> instance to be persisted.</param>
        public Task InsertAsync(Entity entity)
        {
            return InsertAsync(entity, CancellationToken.None);
        }

        /// <summary>
        /// Persists a new entity model.
        /// </summary>
        /// <param name="entity">The new <see cref="Entity"/> instance to be persisted.</param>
        /// <param name="cancellationToken">A token used for cancelling propagation.</param>
        public Task InsertAsync(Entity entity, CancellationToken cancellationToken)
        {
            EnlistWithCurrentTransactionScope();

            return Transaction != null ?
                Collection.InsertOneAsync(Transaction.Session, entity, null, cancellationToken)
                : Collection.InsertOneAsync(entity, null, cancellationToken);
        }

        /// <summary>
        /// Updates an existing persisted entity.
        /// </summary>
        /// <param name="entityToUpdate">The <see cref="Entity"/> instance to be updated.</param>
        public void Update(Entity entityToUpdate)
        {
            EnlistWithCurrentTransactionScope();

            if (Transaction != null)
            {
                Collection.ReplaceOne(Transaction.Session, GetIdFilter(entityToUpdate), entityToUpdate);
            }
            else
            {
                Collection.ReplaceOne(GetIdFilter(entityToUpdate), entityToUpdate);
            }
        }

        /// <summary>
        /// Updates an existing persisted entity.
        /// </summary>
        /// <param name="entityToUpdate">The <see cref="Entity"/> instance to be updated.</param>
        public Task UpdateAsync(Entity entityToUpdate)
        {
            return UpdateAsync(entityToUpdate, CancellationToken.None);
        }

        /// <summary>
        /// Updates an existing persisted entity.
        /// </summary>
        /// <param name="entityToUpdate">The <see cref="Entity"/> instance to be updated.</param>
        /// <param name="cancellationToken">A token used for cancelling propagation.</param>
        public Task UpdateAsync(Entity entityToUpdate, CancellationToken cancellationToken)
        {
            EnlistWithCurrentTransactionScope();

            return Transaction != null ? 
                Collection.ReplaceOneAsync(Transaction.Session, GetIdFilter(entityToUpdate), entityToUpdate, cancellationToken: cancellationToken)
                : Collection.ReplaceOneAsync(GetIdFilter(entityToUpdate), entityToUpdate, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="id">The primary key of the <see cref="Entity"/> to be deleted.</param>
        public void Delete(Key id)
        {
            EnlistWithCurrentTransactionScope();

            if (Transaction != null)
            {
                Collection.DeleteOne(Transaction.Session, GetIdFilter(id));
            }
            else
            {
                Collection.DeleteOne(GetIdFilter(id));
            }
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="id">The primary key of the <see cref="Entity"/> to be deleted.</param>
        public Task DeleteAsync(Key id)
        {
            return DeleteAsync(id, CancellationToken.None);
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="id">The primary key of the <see cref="Entity"/> to be deleted.</param>
        /// <param name="cancellationToken">A token used for cancelling propagation.</param>
        public Task DeleteAsync(Key id, CancellationToken cancellationToken)
        {
            EnlistWithCurrentTransactionScope();

            return Transaction != null ?
                Collection.DeleteOneAsync(Transaction.Session, GetIdFilter(id), null, cancellationToken)
                : Collection.DeleteOneAsync(GetIdFilter(id), cancellationToken);
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="entityToDelete">The <see cref="Entity"/> instance to be deleted.</param>
        public void Delete(Entity entityToDelete)
        {
            EnlistWithCurrentTransactionScope();

            if (Transaction != null)
            {
                Collection.DeleteOne(Transaction.Session, GetIdFilter(entityToDelete));
            }
            else
            {
                Collection.DeleteOne(GetIdFilter(entityToDelete));
            }
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="entityToDelete">The <see cref="Entity"/> instance to be deleted.</param>
        public Task DeleteAsync(Entity entityToDelete)
        {
            return DeleteAsync(entityToDelete, CancellationToken.None);
        }

        /// <summary>
        /// Deletes an existing entity.
        /// </summary>
        /// <param name="entityToDelete">The <see cref="Entity"/> instance to be deleted.</param>
        /// <param name="cancellationToken">A token used for cancelling propagation.</param>
        public Task DeleteAsync(Entity entityToDelete, CancellationToken cancellationToken)
        {
            EnlistWithCurrentTransactionScope();

            if (Transaction != null)
            {
                return Collection.DeleteOneAsync(Transaction.Session, GetIdFilter(entityToDelete), null, cancellationToken);
            }
            else
            {
                return Collection.DeleteOneAsync(GetIdFilter(entityToDelete), cancellationToken);
            }
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
            EnlistWithCurrentTransactionScope();

            if (Transaction != null)
            {
                return GlobalFilter != null ? Collection.AsQueryable(Transaction.Session).Where(GlobalFilter) : Collection.AsQueryable(Transaction.Session);
            }
            else
            {
                return GlobalFilter != null ? Collection.AsQueryable().Where(GlobalFilter) : Collection.AsQueryable();
            }
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
            return _dataPager.GetPagedData(GetQueryable(), settings, _preConditionsToPagedDataFilterDelegate(settings), _extraPagedDataFilterDelegate(settings));
        }
    }
}
