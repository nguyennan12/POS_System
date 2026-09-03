using POS.Contracts.V1.Common;

namespace POS.Contracts.V1.Inventory;

public record InventoryFilterRequest(
    Guid? SkuId = null,
    Guid? CategoryId = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20
) : PagedRequest(PageNumber, PageSize);

public record BatchFilterRequest(
    Guid? SkuId = null,
    DateOnly? ExpiryBefore = null
);

public record DisposeStockRequest(
    Guid SkuId,
    decimal Qty,
    string Note
);

public record StockInVoucherFilterRequest(
    Guid? SupplierId = null,
    string? Status = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int PageNumber = 1,
    int PageSize = 20
) : PagedRequest(PageNumber, PageSize);

public record StockInVoucherItemRequest(
    Guid SkuId,
    decimal Qty,
    decimal UnitPrice
);

public record CreateStockInVoucherRequest(
    Guid SupplierId,
    IReadOnlyList<StockInVoucherItemRequest> Items,
    string? Note = null
);

public record UpdateStockInVoucherStatusRequest(
    string Status
);

public record CreateStockTakeRequest(
    string? Note = null
);

public record StockTakeItemUpdateRequest(
    Guid SkuId,
    decimal ActualQty,
    string? Note = null
);

public record UpdateStockTakeItemsRequest(
    IReadOnlyList<StockTakeItemUpdateRequest> Items
);
