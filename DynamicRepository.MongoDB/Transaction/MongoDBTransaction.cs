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
            if (HasBeenDisposed) return;

            Session.CommitTransaction();
        }

        public Task CommitAsync(CancellationToken cancellation = default)
        {
            if (HasBeenDisposed) return Task.CompletedTask;

            return Session.CommitTransactionAsync(cancellation);
        }

        public void Abort()
        {
            if (HasBeenDisposed) return;

            Session.AbortTransaction();
        }

        public Task AbortAsync(CancellationToken cancellation = default)
        {
            if (HasBeenDisposed) return Task.CompletedTask;

            return Session.AbortTransactionAsync(cancellation);
        }

        public void Dispose()
        {
            HasBeenDisposed = true;

            Session.Dispose();
        }
    }
}
