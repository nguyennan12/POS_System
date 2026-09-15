using POS.Domain.Common;

namespace POS.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    // Executes application-level read/check/write in a serializable transaction.
    // The operation calls SaveChangesAsync; a failure result is rolled back, never retried.
    Task<Result<T>> ExecuteSerializableAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken = default);
}
