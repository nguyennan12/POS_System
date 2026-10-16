using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Products.Queries.GetProductKpiSummary;

internal sealed class GetProductKpiSummaryQueryHandler(
    IProductRepository productRepository,
    IStoreAccessPolicy storeAccessPolicy) : IQueryHandler<GetProductKpiSummaryQuery, ProductKpiSummaryDto>
{
    public async Task<Result<ProductKpiSummaryDto>> Handle(
        GetProductKpiSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var storeContext = storeAccessPolicy.EnforceStoreAccess(query.StoreId);
        if (storeContext.IsFailure) return storeContext.Error;

        var (totalProducts, activeProducts, totalSkus, totalCategories) =
            await productRepository.GetProductKpiSummaryAsync(storeContext.Value, cancellationToken);

        return new ProductKpiSummaryDto(totalProducts, activeProducts, totalSkus, totalCategories);
    }
}
