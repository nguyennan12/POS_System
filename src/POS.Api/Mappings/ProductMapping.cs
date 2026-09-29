using System.Text.Json;
using POS.Application.UseCases.Products;
using POS.Contracts.V1.Products;

namespace POS.Api.Mappings;

public static class ProductMapping
{
    // ── Product mappings ──────────────────────────────────────────────────────

    public static ProductSummaryResponse ToResponse(this ProductSummaryDto dto) => new(
        dto.Id,
        dto.CategoryId,
        dto.CategoryName,
        dto.Name,
        dto.Brand,
        dto.BaseUnit,
        dto.ImageUrl,
        dto.Status,
        dto.SkuCount,
        new DateTimeOffset(dto.CreatedAt, TimeSpan.Zero)
    );

    public static ProductDetailResponse ToResponse(this ProductDetailDto dto) => new(
        dto.Id,
        dto.CategoryId,
        dto.CategoryName,
        dto.Name,
        dto.Description,
        dto.Brand,
        dto.BaseUnit,
        dto.ImageUrl,
        dto.Status,
        dto.Skus.Select(s => new SkuResponse(
            s.Id, s.ProductId, s.SkuCode, s.Barcode, s.Attributes, 
            s.CostPrice, s.SellPrice, s.TaxRate, s.IsActive, 
            new DateTimeOffset(s.CreatedAt, TimeSpan.Zero)
        )).ToList().AsReadOnly(),
        new DateTimeOffset(dto.CreatedAt, TimeSpan.Zero),
        new DateTimeOffset(dto.UpdatedAt, TimeSpan.Zero)
    );

    // ── SKU mappings ──────────────────────────────────────────────────────────

    public static UnitConversionResponse ToResponse(this UnitConversionDto dto) => new(
        dto.Id,
        dto.SkuId,
        dto.UnitName,
        dto.ConversionFactor,
        dto.SellPrice
    );

    public static SkuResponse ToResponse(this SkuDto dto) => new(
        dto.Id,
        dto.ProductId,
        dto.SkuCode,
        dto.Barcode,
        dto.Attributes,
        dto.CostPrice,
        dto.SellPrice,
        dto.TaxRate,
        dto.IsActive,
        new DateTimeOffset(dto.CreatedAt, TimeSpan.Zero)
    );

    public static SkuDetailResponse ToResponse(this SkuDetailDto dto) => new(
        dto.Id,
        dto.ProductId,
        dto.ProductName,
        dto.SkuCode,
        dto.Barcode,
        dto.Attributes,
        dto.CostPrice,
        dto.SellPrice,
        dto.TaxRate,
        dto.IsActive,
        dto.QtyOnHand,
        dto.UnitConversions.Select(uc => uc.ToResponse()).ToList().AsReadOnly(),
        new DateTimeOffset(dto.CreatedAt, TimeSpan.Zero),
        new DateTimeOffset(dto.UpdatedAt, TimeSpan.Zero)
    );

    public static SkuBarcodeLookupResponse ToResponse(this SkuBarcodeLookupDto dto) => new(
        dto.Id,
        dto.ProductId,
        dto.ProductName,
        dto.SkuCode,
        dto.Barcode,
        dto.SellPrice,
        dto.TaxRate,
        dto.QtyOnHand,
        dto.BaseUnit,
        dto.UnitConversions.Select(uc => uc.ToResponse()).ToList().AsReadOnly()
    );
}
