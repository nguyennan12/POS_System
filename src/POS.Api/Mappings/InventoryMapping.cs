using POS.Application.UseCases.Inventory;
using POS.Contracts.V1.Inventory;

namespace POS.Api.Mappings;

public static class InventoryMapping
{
    // ── StockIn Voucher ───────────────────────────────────────────────────────

    public static StockInVoucherItemResponse ToResponse(this StockInVoucherItemDto dto) => new(
        dto.Id,
        dto.SkuId,
        dto.SkuCode,
        dto.ProductName,
        dto.Qty,
        dto.UnitPrice,
        dto.TotalPrice,
        dto.BatchNo,
        dto.ExpiryDate
    );

    public static StockInVoucherSummaryResponse ToResponse(this StockInVoucherSummaryDto dto) => new(
        dto.Id,
        dto.StoreId,
        dto.SupplierId,
        dto.SupplierName,
        dto.TotalAmount,
        dto.Status,
        dto.Note,
        dto.CreatedBy,
        dto.CreatedAt
    );

    public static StockInVoucherDetailResponse ToResponse(this StockInVoucherDetailDto dto) => new(
        dto.Id,
        dto.StoreId,
        dto.SupplierId,
        dto.SupplierName,
        dto.TotalAmount,
        dto.Status,
        dto.Note,
        dto.CreatedBy,
        dto.CreatedAt,
        dto.Items.Select(i => i.ToResponse()).ToList().AsReadOnly()
    );

    // ── Stock Entry ───────────────────────────────────────────────────────────

    public static StockEntryResponse ToResponse(this StockEntryDto dto) => new(
        dto.Id,
        dto.SkuId,
        dto.SkuCode,
        dto.Barcode,
        dto.ProductName,
        dto.QtyOnHand,
        dto.MinStock,
        dto.LastUpdated
    );

    public static StockAlertResponse ToResponse(this StockAlertDto dto) => new(
        dto.SkuId,
        dto.SkuCode,
        dto.Barcode,
        dto.ProductName,
        dto.QtyOnHand,
        dto.MinStock,
        dto.AlertType
    );

    public static StockBatchResponse ToResponse(this StockBatchDto dto) => new(
        dto.Id,
        dto.SkuId,
        dto.SkuCode,
        dto.ProductName,
        dto.BatchNo,
        dto.Qty,
        dto.ExpiryDate,
        dto.ReceivedAt
    );

    public static StockTransactionResponse ToResponse(this StockTransactionDto dto) => new(
        dto.Id,
        dto.StoreId,
        dto.SkuId,
        dto.SkuCode,
        dto.Type,
        dto.Qty,
        dto.OrderId,
        dto.StockInVoucherId,
        dto.Note,
        dto.CreatedBy,
        dto.CreatedAt
    );

    // ── Stock Take ────────────────────────────────────────────────────────────

    public static StockTakeItemResponse ToResponse(this StockTakeItemDto dto) => new(
        dto.Id,
        dto.SkuId,
        dto.SkuCode,
        dto.Barcode,
        dto.ProductName,
        dto.SystemQty,
        dto.ActualQty,
        dto.DiffQty,
        dto.Note
    );

    public static StockTakeSummaryResponse ToResponse(this StockTakeSummaryDto dto) => new(
        dto.Id,
        dto.StoreId,
        dto.Status,
        dto.Note,
        dto.CreatedBy,
        dto.ApprovedBy,
        dto.CreatedAt,
        dto.ApprovedAt
    );

    public static StockTakeDetailResponse ToResponse(this StockTakeDetailDto dto) => new(
        dto.Id,
        dto.StoreId,
        dto.Status,
        dto.Note,
        dto.CreatedBy,
        dto.ApprovedBy,
        dto.CreatedAt,
        dto.ApprovedAt,
        dto.Items.Select(i => i.ToResponse()).ToList().AsReadOnly()
    );
}
