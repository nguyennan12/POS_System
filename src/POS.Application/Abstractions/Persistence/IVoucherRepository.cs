using POS.Domain.Promotions;

namespace POS.Application.Abstractions.Persistence;

public interface IVoucherRepository
{
    Task<Voucher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Voucher?> GetByIdWithPromotionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Voucher?> GetByCodeWithPromotionAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> IsCodeUniqueAsync(string code, Guid? excludeVoucherId = null, CancellationToken cancellationToken = default);
    Task<int> GetCustomerUsageCountAsync(Guid voucherId, Guid customerId, CancellationToken cancellationToken = default);
    Task<(List<Voucher> Items, int TotalCount)> GetPagedAsync(
        string? code,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<(List<Voucher> Items, int TotalCount)> GetPagedByPromotionIdAsync(
        Guid promotionId,
        string? code,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(Voucher voucher, CancellationToken cancellationToken = default);
    void Remove(Voucher voucher);
}
