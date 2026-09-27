using POS.Domain.Promotions;

namespace POS.Application.Abstractions.Persistence;

public interface IVoucherRepository
{
    Task<Voucher?> GetByIdWithPromotionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Voucher?> GetByCodeWithPromotionAsync(string code, CancellationToken cancellationToken = default);
    Task<int> GetCustomerUsageCountAsync(Guid voucherId, Guid customerId, CancellationToken cancellationToken = default);
}
