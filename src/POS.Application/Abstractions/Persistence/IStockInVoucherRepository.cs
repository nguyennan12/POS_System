using POS.Domain.Inventory.StockIn;

namespace POS.Application.Abstractions.Persistence;

public interface IStockInVoucherRepository
{
    Task<StockInVoucher?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(List<StockInVoucher> Items, int TotalCount)> GetPagedAsync(
        Guid storeId,
        Guid? supplierId,
        string? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(StockInVoucher voucher, CancellationToken cancellationToken = default);
}
