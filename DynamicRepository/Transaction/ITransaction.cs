using System;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicRepository.Transaction
{
    public interface ITransaction : IDisposable
    {
        void Commit();
        Task CommitAsync(CancellationToken cancellation = default);
        void Abort();
        Task AbortAsync(CancellationToken cancellation = default);
    }
}
