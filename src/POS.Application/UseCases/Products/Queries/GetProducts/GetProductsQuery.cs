using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Products.Queries.GetProducts;

public record GetProductsQuery(
    string? Search,
    Guid? CategoryId,
    string? Status,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagedProductList>;

public record PagedProductList(
    IReadOnlyList<ProductSummaryDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
