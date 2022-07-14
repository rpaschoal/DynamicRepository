using System.Transactions;

namespace DynamicRepository.Transaction
{
    public interface ITransactionRegister
    {
        TransactionScope StartTransactionScope();
        ITransaction StartTransaction();
        void RegisterTransaction(ITransaction transaction);
    }
}
