using System.Text.Json;

namespace POS.Application.UseCases.Products;

public record ProductSummaryDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string? Brand,
    string BaseUnit,
    string? ImageUrl,
    string Status,
    int SkuCount,
    DateTime CreatedAt);

public record ProductDetailDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string? Description,
    string? Brand,
    string BaseUnit,
    string? ImageUrl,
    string Status,
    List<SkuDetailDto> Skus,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record UnitConversionDto(
    Guid Id,
    Guid SkuId,
    string UnitName,
    decimal ConversionFactor,
    decimal SellPrice);

public record SkuDto(
    Guid Id,
    Guid ProductId,
    string SkuCode,
    string Barcode,
    JsonElement? Attributes,
    decimal CostPrice,
    decimal SellPrice,
    decimal TaxRate,
    bool IsActive,
    DateTime CreatedAt);

public record SkuDetailDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string SkuCode,
    string Barcode,
    JsonElement? Attributes,
    decimal CostPrice,
    decimal SellPrice,
    decimal TaxRate,
    bool IsActive,
    decimal QtyOnHand,
    List<UnitConversionDto> UnitConversions,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record SkuBarcodeLookupDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string SkuCode,
    string Barcode,
    decimal SellPrice,
    decimal TaxRate,
    decimal QtyOnHand,
    string BaseUnit,
    List<UnitConversionDto> UnitConversions);

public record PriceListDto(
    Guid Id,
    Guid StoreId,
    Guid SkuId,
    decimal Price,
    DateTime ValidFrom,
    DateTime? ValidTo,
    string? CustomerGroup,
    Guid CreatedBy,
    DateTime CreatedAt);
