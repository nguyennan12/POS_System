namespace POS.Contracts.V1.Products;

public record UnitConversionResponse(
    Guid Id,
    Guid SkuId,
    string UnitName,
    decimal ConversionFactor,
    decimal SellPrice
);

public record PriceListResponse(
    Guid Id,
    Guid StoreId,
    Guid SkuId,
    decimal Price,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    string? CustomerGroup,
    Guid CreatedBy,
    DateTimeOffset CreatedAt
);

public record SkuResponse(
    Guid Id,
    Guid ProductId,
    string SkuCode,
    string Barcode,
    string? AttributesJson,
    decimal CostPrice,
    decimal SellPrice,
    decimal TaxRate,
    bool IsActive,
    DateTimeOffset CreatedAt
);

public record SkuDetailResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string SkuCode,
    string Barcode,
    string? AttributesJson,
    decimal CostPrice,
    decimal SellPrice,
    decimal TaxRate,
    bool IsActive,
    decimal QtyOnHand,
    IReadOnlyList<UnitConversionResponse> UnitConversions,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record SkuBarcodeLookupResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string SkuCode,
    string Barcode,
    decimal SellPrice,
    decimal TaxRate,
    decimal QtyOnHand,
    string BaseUnit,
    IReadOnlyList<UnitConversionResponse> UnitConversions
);
