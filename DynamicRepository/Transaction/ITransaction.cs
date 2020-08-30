using System;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicRepository.Transaction
{
    public interface ITransaction : IDisposable
    {
        Task CommitAsync(CancellationToken cancellation = default);
        Task AbortAsync(CancellationToken cancellation = default);
    }
}
