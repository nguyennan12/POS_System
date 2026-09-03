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
