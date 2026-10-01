using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Products.Queries.GetProducts;

internal sealed class GetProductsQueryHandler(
    IProductRepository productRepository,
    ICurrentUser currentUser) : IQueryHandler<GetProductsQuery, PagedProductList>
{
    public async Task<Result<PagedProductList>> Handle(
        GetProductsQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.StoreId is null)
            return ProductErrors.StoreRequired;

        var storeId = currentUser.StoreId.Value;

        var (rows, total) = await productRepository.GetPagedAsync(
            storeId,
            query.Search,
            query.CategoryId,
            query.Status,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var dtos = rows.Select(row => new ProductSummaryDto(
            row.Product.Id,
            row.Product.CategoryId,
            row.CategoryName,
            row.Product.Name,
            row.Product.Brand,
            row.Product.BaseUnit,
            row.Product.ImageUrl,
            row.Product.Status.ToString(),
            row.SkuCount,
            row.Product.CreatedAt
        )).ToList();

        return new PagedProductList(dtos, total, query.PageNumber, query.PageSize);
    }
}
