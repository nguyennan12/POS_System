using POS.Domain.Common;

namespace POS.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    // Includes the reads/checks and the write in one serializable transaction.
    // The operation calls SaveChangesAsync; a failure result is rolled back, never retried.
    Task<Result<T>> ExecuteSerializableAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken = default);
}
