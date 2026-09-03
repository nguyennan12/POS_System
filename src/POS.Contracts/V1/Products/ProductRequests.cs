using POS.Contracts.V1.Common;

namespace POS.Contracts.V1.Products;

public record ProductFilterRequest(
    string? Search = null,
    Guid? CategoryId = null,
    string? Status = null,
    int PageNumber = 1,
    int PageSize = 20
) : PagedRequest(PageNumber, PageSize);

public record CreateProductRequest(
    string Name,
    Guid CategoryId,
    string BaseUnit,
    string? Description = null,
    string? Brand = null,
    string? ImageUrl = null,
    IReadOnlyList<CreateSkuRequest>? Skus = null
);

public record UpdateProductRequest(
    string Name,
    Guid CategoryId,
    string BaseUnit,
    string? Description = null,
    string? Brand = null,
    string? ImageUrl = null,
    string Status = "Active"
);

public record BulkImportProductRow(
    string Name,
    string CategoryName,
    string BaseUnit,
    string SkuCode,
    string Barcode,
    decimal CostPrice,
    decimal SellPrice,
    decimal TaxRate,
    string? Brand = null,
    string? AttributesJson = null
);
