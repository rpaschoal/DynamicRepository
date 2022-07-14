using DynamicRepository.Transaction;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicRepository.MongoDB.Transaction
{
    public sealed class MongoDBTransaction : ITransaction, IDisposable
    {
        internal IClientSessionHandle Session { get; }

        internal bool HasBeenDisposed { get; private set; }

        public MongoDBTransaction(IMongoClient client, ClientSessionOptions clientSessionOptions = null)
        {
            Session = client.StartSession(clientSessionOptions);

            Session.StartTransaction();
        }

        public void Commit()
        {
            Session.CommitTransaction();
        }

        public Task CommitAsync(CancellationToken cancellation = default)
        {
            return Session.CommitTransactionAsync(cancellation);
        }

        public void Abort()
        {
            Session.AbortTransaction();
        }

        public Task AbortAsync(CancellationToken cancellation = default)
        {
            return Session.AbortTransactionAsync(cancellation);
        }

        public void Dispose()
        {
            HasBeenDisposed = true;

            Session.Dispose();
        }
    }
}
