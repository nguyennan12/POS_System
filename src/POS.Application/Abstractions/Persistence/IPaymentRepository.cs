using POS.Domain.Orders;

namespace POS.Application.Abstractions.Persistence;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdWithOrderAsync(Guid id, CancellationToken cancellationToken = default);
}
