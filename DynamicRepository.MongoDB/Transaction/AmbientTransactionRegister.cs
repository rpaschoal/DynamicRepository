using DynamicRepository.Transaction;
using System.Collections.Concurrent;

namespace DynamicRepository.MongoDB.Transaction
{
    internal static class AmbientTransactionRegister
    {
        internal static ConcurrentDictionary<string, ITransaction> AmbientTransactions = new ConcurrentDictionary<string, ITransaction>();
    }
}
