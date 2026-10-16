using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Products.Queries.GetPosCatalog;

public record GetPosCatalogQuery(
    Guid? CategoryId,
    string? Search,
    int PageNumber,
    int PageSize,
    Guid? StoreId = null
) : IQuery<PagedPosCatalogList>;
