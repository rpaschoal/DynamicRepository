namespace DynamicRepository.Transaction
{
    public interface ITransactionRegister
    {
        ITransaction StartTransaction();
        void RegisterTransaction(ITransaction transaction);
    }
}
