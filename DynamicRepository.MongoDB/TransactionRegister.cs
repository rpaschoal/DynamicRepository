using DynamicRepository.Transaction;
using System.Collections.Concurrent;

namespace DynamicRepository.MongoDB
{
    internal static class TransactionRegister
    {
        internal static ConcurrentDictionary<string, ITransaction> AmbientTransactions = new ConcurrentDictionary<string, ITransaction>();
    }
}
