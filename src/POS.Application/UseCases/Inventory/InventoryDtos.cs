using POS.Domain.Inventory.Stock;
using POS.Domain.Inventory.StockIn;

namespace POS.Application.UseCases.Inventory;

// ── StockIn Voucher DTOs ──────────────────────────────────────────────────────

public record StockInVoucherItemDto(
    Guid Id,
    Guid SkuId,
    string SkuCode,
    string ProductName,
    decimal Qty,
    decimal UnitPrice,
    decimal TotalPrice,
    string? BatchNo,
    DateOnly? ExpiryDate
);

public record StockInVoucherSummaryDto(
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

public record StockInVoucherDetailDto(
    Guid Id,
    Guid StoreId,
    Guid SupplierId,
    string SupplierName,
    decimal TotalAmount,
    string Status,
    string? Note,
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    IReadOnlyList<StockInVoucherItemDto> Items
);

// ── Stock Entry DTOs ──────────────────────────────────────────────────────────

public record StockEntryDto(
    Guid Id,
    Guid SkuId,
    string SkuCode,
    string Barcode,
    string ProductName,
    decimal QtyOnHand,
    decimal MinStock,
    decimal AverageCost,
    DateTimeOffset LastUpdated
);

public record StockAlertDto(
    Guid SkuId,
    string SkuCode,
    string Barcode,
    string ProductName,
    decimal QtyOnHand,
    decimal MinStock,
    string AlertType  // "MinStock" | "NearExpiry"
);

public record StockBatchDto(
    Guid Id,
    Guid SkuId,
    string SkuCode,
    string ProductName,
    string BatchNo,
    decimal Qty,
    DateOnly? ExpiryDate,
    DateTimeOffset ReceivedAt
);

public record StockTransactionDto(
    Guid Id,
    Guid StoreId,
    Guid SkuId,
    string SkuCode,
    string Type,
    decimal Qty,
    decimal? UnitCost,
    Guid? OrderId,
    Guid? StockInVoucherId,
    string? Note,
    Guid CreatedBy,
    DateTimeOffset CreatedAt
);

// ── Extension methods ─────────────────────────────────────────────────────────

public static class InventoryDtoExtensions
{
    public static StockInVoucherSummaryDto ToSummaryDto(this StockInVoucher v) => new(
        v.Id,
        v.StoreId,
        v.SupplierId,
        v.Supplier?.Name ?? string.Empty,
        v.TotalAmount,
        v.Status.ToString(),
        v.Note,
        v.CreatedBy,
        new DateTimeOffset(v.CreatedAt, TimeSpan.Zero)
    );

    public static StockInVoucherDetailDto ToDetailDto(this StockInVoucher v) => new(
        v.Id,
        v.StoreId,
        v.SupplierId,
        v.Supplier?.Name ?? string.Empty,
        v.TotalAmount,
        v.Status.ToString(),
        v.Note,
        v.CreatedBy,
        new DateTimeOffset(v.CreatedAt, TimeSpan.Zero),
        v.Items.Select(i => i.ToDto()).ToList().AsReadOnly()
    );

    public static StockInVoucherItemDto ToDto(this StockInVoucherItem i) => new(
        i.Id,
        i.SkuId,
        i.Sku?.SkuCode ?? string.Empty,
        i.Sku?.Product?.Name ?? string.Empty,
        i.Qty,
        i.UnitPrice,
        i.TotalPrice,
        i.BatchNo,
        i.ExpiryDate
    );

    public static StockEntryDto ToDto(this StockEntry e) => new(
        e.Id,
        e.SkuId,
        e.Sku?.SkuCode ?? string.Empty,
        e.Sku?.Barcode ?? string.Empty,
        e.Sku?.Product?.Name ?? string.Empty,
        e.QtyOnHand,
        e.MinStock,
        e.AverageCost,
        new DateTimeOffset(e.LastUpdated, TimeSpan.Zero)
    );

    public static StockBatchDto ToDto(this StockBatch b) => new(
        b.Id,
        b.SkuId,
        b.Sku?.SkuCode ?? string.Empty,
        b.Sku?.Product?.Name ?? string.Empty,
        b.BatchNo,
        b.Qty,
        b.ExpiryDate,
        new DateTimeOffset(b.ReceivedAt, TimeSpan.Zero)
    );

    public static StockTransactionDto ToDto(this StockTransaction t) => new(
        t.Id,
        t.StoreId,
        t.SkuId,
        t.Sku?.SkuCode ?? string.Empty,
        t.Type.ToString(),
        t.Qty,
        t.UnitCost,
        t.OrderId,
        t.StockInVoucherId,
        t.Note,
        t.CreatedBy,
        new DateTimeOffset(t.CreatedAt, TimeSpan.Zero)
    );
}
