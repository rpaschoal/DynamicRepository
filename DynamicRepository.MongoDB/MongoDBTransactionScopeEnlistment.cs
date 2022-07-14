using DynamicRepository.Transaction;
using System.Transactions;

namespace DynamicRepository.MongoDB
{
    internal class MongoDBTransactionScopeEnlistment : IEnlistmentNotification
    {
        private readonly ITransaction _transaction;

        public MongoDBTransactionScopeEnlistment(ITransaction transaction)
        {
            _transaction = transaction;
        }

        public void Commit(Enlistment enlistment)
        {
            _transaction.Commit();
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Rollback(Enlistment enlistment)
        {
            _transaction.Abort();
        }
    }
}