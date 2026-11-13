namespace POS.Contracts.V1.Products;

public record ProductSummaryResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string? Brand,
    string BaseUnit,
    string? ImageUrl,
    string Status,
    int SkuCount,
    DateTimeOffset CreatedAt
);

public record ProductDetailResponse(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string? Description,
    string? Brand,
    string BaseUnit,
    string? ImageUrl,
    string Status,
    IReadOnlyList<SkuResponse> Skus,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record BulkImportResultResponse(
    int TotalRows,
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<string> Errors
);

public record PosCatalogItemResponse(
    Guid SkuId,
    Guid ProductId,
    string ProductName,
    string SkuCode,
    string Barcode,
    decimal SellPrice,
    decimal CostPrice,
    decimal TaxRate,
    decimal QtyOnHand,
    string BaseUnit,
    Guid CategoryId,
    string CategoryName,
    string? ImageUrl
);

public record ProductKpiSummaryResponse(
    int TotalProducts,
    int ActiveProducts,
    int TotalSkus,
    int TotalCategories
);

public record ProductImportPreviewRowDto(
    int RowNumber,
    string? Name,
    string? CategoryName,
    string? BaseUnit,
    string? SkuCode,
    string? Barcode,
    decimal? CostPrice,
    decimal? SellPrice,
    decimal? TaxRate,
    decimal? InitialStock,
    decimal? MinStock,
    string? BatchNo,
    DateOnly? ExpiryDate,
    bool IsValid,
    string? ErrorMessage
);

public record ProductImportPreviewResponse(
    string FileName,
    long FileSizeBytes,
    int TotalRows,
    int ValidRowsCount,
    int ErrorRowsCount,
    IReadOnlyList<ProductImportPreviewRowDto> PreviewItems,
    IReadOnlyList<string> Errors
);
