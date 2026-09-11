using System.Text.Json;

namespace POS.Contracts.V1.Products;

public record CreateSkuRequest(
    string SkuCode,
    string Barcode,
    decimal CostPrice,
    decimal SellPrice,
    decimal TaxRate = 0,
    JsonElement? Attributes = null
);

public record UpdateSkuRequest(
    string SkuCode,
    string Barcode,
    decimal CostPrice,
    decimal SellPrice,
    decimal TaxRate,
    bool IsActive,
    JsonElement? Attributes = null
);

public record CreateUnitConversionRequest(
    string UnitName,
    decimal ConversionFactor,
    decimal SellPrice
);

public record UpdateUnitConversionRequest(
    string UnitName,
    decimal ConversionFactor,
    decimal SellPrice
);

public record CreatePriceListRequest(
    decimal Price,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo = null,
    string? CustomerGroup = null
);
