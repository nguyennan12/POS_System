using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Products.Queries.GetPosCatalog;

internal sealed class GetPosCatalogQueryHandler(
    ISkuRepository skuRepository,
    ICurrentUser currentUser) : IQueryHandler<GetPosCatalogQuery, PagedPosCatalogList>
{
    public async Task<Result<PagedPosCatalogList>> Handle(
        GetPosCatalogQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.StoreId is null)
            return ProductErrors.StoreRequired;

        var storeId = currentUser.StoreId.Value;

        var pageNumber = Math.Max(1, query.PageNumber);
        var pageSize = Math.Min(200, Math.Max(1, query.PageSize));

        var (items, total) = await skuRepository.GetPosCatalogAsync(
            storeId,
            query.CategoryId,
            query.Search,
            pageNumber,
            pageSize,
            cancellationToken);

        return new PagedPosCatalogList(items, total, pageNumber, pageSize);
    }
}
