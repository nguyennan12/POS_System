using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Payments;

internal static class PaymentTransactions
{
    public static async Task<Result<T>> ExecuteAsync<T>(IUnitOfWork uow,
        Func<CancellationToken, Task<Result<T>>> operation, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            Result<T> result;
            try { result = await uow.ExecuteSerializableAsync(operation, ct); }
            catch (PersistenceConflictException ex) when (attempt < 2 && ex.ConstraintName is
                "IX_payments_method_transaction_ref" or "IX_payments_method_merchant_reference" or "IX_payments_pending_qr")
            {
                // The aborted transaction has been disposed and its tracker cleared.
                // Replay reads: a duplicate on the same payment becomes an ack; another payment is rejected.
                continue;
            }
            catch (PersistenceConflictException)
            {
                return new Error(ErrorType.Invalid, "Persistence.ConcurrentModification", "Thanh toán đã thay đổi. Vui lòng thử lại.");
            }
            if (attempt < 2 && result.IsFailure && result.Error.Code == "Persistence.ConcurrentModification") continue;
            return result;
        }
    }
}
