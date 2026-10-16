namespace POS.Application.UseCases.Products.Queries.GetProductKpiSummary;

public record ProductKpiSummaryDto(
    int TotalProducts,
    int ActiveProducts,
    int TotalSkus,
    int TotalCategories
);
