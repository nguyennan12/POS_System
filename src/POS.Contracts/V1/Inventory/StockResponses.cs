namespace POS.Contracts.V1.Inventory;

public record StockEntryResponse(
    Guid Id,
    Guid SkuId,
    string SkuCode,
    string Barcode,
    string ProductName,
    decimal QtyOnHand,
    decimal MinStock,
    DateTimeOffset LastUpdated
);

public record StockAlertResponse(
    Guid SkuId,
    string SkuCode,
    string Barcode,
    string ProductName,
    decimal QtyOnHand,
    decimal MinStock,
    string AlertType
);

public record StockBatchResponse(
    Guid Id,
    Guid SkuId,
    string SkuCode,
    string ProductName,
    string BatchNo,
    decimal Qty,
    DateOnly? ExpiryDate,
    DateTimeOffset ReceivedAt
);

public record StockTransactionResponse(
    Guid Id,
    Guid StoreId,
    Guid SkuId,
    string SkuCode,
    string Type,
    decimal Qty,
    Guid? OrderId,
    Guid? StockInVoucherId,
    string? Note,
    Guid CreatedBy,
    DateTimeOffset CreatedAt
);

public record StockInVoucherItemResponse(
    Guid Id,
    Guid SkuId,
    string SkuCode,
    string ProductName,
    decimal Qty,
    decimal UnitPrice,
    decimal TotalPrice
);

public record StockInVoucherSummaryResponse(
    Guid Id,
    Guid StoreId,
    Guid SupplierId,
    string SupplierName,
    decimal TotalAmount,
    string Status,
    string? Note,
    Guid CreatedBy,
    DateTimeOffset CreatedAt
);

public record StockInVoucherDetailResponse(
    Guid Id,
    Guid StoreId,
    Guid SupplierId,
    string SupplierName,
    decimal TotalAmount,
    string Status,
    string? Note,
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    IReadOnlyList<StockInVoucherItemResponse> Items
);

public record StockTakeItemResponse(
    Guid Id,
    Guid SkuId,
    string SkuCode,
    string ProductName,
    decimal SystemQty,
    decimal ActualQty,
    decimal DiffQty,
    string? Note
);

public record StockTakeSummaryResponse(
    Guid Id,
    Guid StoreId,
    string Status,
    string? Note,
    Guid CreatedBy,
    Guid? ApprovedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ApprovedAt
);

public record StockTakeDetailResponse(
    Guid Id,
    Guid StoreId,
    string Status,
    string? Note,
    Guid CreatedBy,
    Guid? ApprovedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ApprovedAt,
    IReadOnlyList<StockTakeItemResponse> Items
);
