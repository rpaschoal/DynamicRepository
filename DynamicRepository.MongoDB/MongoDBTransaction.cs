using DynamicRepository.Transaction;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicRepository.MongoDB
{
    public sealed class MongoDBTransaction : ITransaction, IDisposable
	{
		internal IClientSessionHandle Session { get; }

		public MongoDBTransaction(IMongoClient client, ClientSessionOptions clientSessionOptions = null)
		{
			Session = client.StartSession(clientSessionOptions);

			Session.StartTransaction();
		}

		public Task CommitAsync(CancellationToken cancellation = default)
		{
			return Session.CommitTransactionAsync(cancellation);
		}

		public Task AbortAsync(CancellationToken cancellation = default)
		{
			return Session.AbortTransactionAsync(cancellation);
		}

		public void Dispose()
		{
			Session.Dispose();
		}
	}
}
