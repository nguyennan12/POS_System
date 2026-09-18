using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Employees;

internal static class EmployeeTransactions
{
    // Each attempt reloads caller, target and all guards in a new serializable transaction.
    // Never reuse authorization decisions/entities from a rolled-back attempt.
    public static async Task<Result<T>> ExecuteAsync<T>(IUnitOfWork unitOfWork,
        Func<CancellationToken, Task<Result<T>>> operation, bool retryOnce, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var result = await unitOfWork.ExecuteSerializableAsync(operation, cancellationToken);
                if (retryOnce && attempt == 0 && result.IsFailure && result.Error.Code == "Persistence.ConcurrentModification")
                    continue;
                return result;
            }
            catch (PersistenceConflictException ex) when (
                ex.ConstraintName is PersistenceConstraints.EmployeeUsernameUnique or PersistenceConstraints.EmployeeNormalizedUsernameUnique)
            {
                return EmployeeErrors.UsernameExists;
            }
            catch (PersistenceConflictException ex) when (ex.ConstraintName == PersistenceConstraints.EmployeeStorePinLookupUnique)
            {
                return EmployeeErrors.PinExists;
            }
        }
    }
}
