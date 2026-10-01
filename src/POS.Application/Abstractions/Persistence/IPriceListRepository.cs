using POS.Domain.Products;

namespace POS.Application.Abstractions.Persistence;

public interface IPriceListRepository
{
    /// <summary>
    /// Kiểm tra xem có bảng giá nào chồng lấn thời gian không, lọc chính xác theo StoreId và NULL CustomerGroup.
    /// </summary>
    Task<bool> IsOverlappingAsync(
        Guid skuId,
        Guid storeId,
        string? customerGroup,
        DateTime validFrom,
        DateTime? validTo,
        CancellationToken cancellationToken = default);

    Task AddAsync(PriceList priceList, CancellationToken cancellationToken = default);
}
