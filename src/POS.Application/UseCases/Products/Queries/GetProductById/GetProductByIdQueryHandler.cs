using System.Text.Json;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Products.Queries.GetProductById;

internal sealed class GetProductByIdQueryHandler(
    IProductRepository productRepository,
    ICurrentUser currentUser) : IQueryHandler<GetProductByIdQuery, ProductDetailDto>
{
    public async Task<Result<ProductDetailDto>> Handle(
        GetProductByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.StoreId is null)
            return ProductErrors.StoreRequired;

        var product = await productRepository.GetByIdWithSkusAsync(query.Id, currentUser.StoreId.Value, cancellationToken);
        if (product is null)
            return ProductErrors.ProductNotFound(query.Id);

        var skuDtos = product.Skus?.Select(s => new SkuDetailDto(
            s.Id,
            s.ProductId,
            product.Name,
            s.SkuCode,
            s.Barcode,
            s.Attributes != null ? JsonSerializer.SerializeToElement(s.Attributes) : null,
            s.CostPrice,
            s.SellPrice,
            s.TaxRate,
            s.IsActive,
            0, // QtyOnHand (Not loading stock info here to keep it simple)
            new List<UnitConversionDto>(), // UnitConversions
            s.CreatedAt,
            s.UpdatedAt
        )).ToList() ?? new List<SkuDetailDto>();

        return new ProductDetailDto(
            product.Id,
            product.CategoryId,
            product.Category?.Name ?? string.Empty,
            product.Name,
            product.Description,
            product.Brand,
            product.BaseUnit,
            product.ImageUrl,
            product.Status.ToString(),
            skuDtos,
            product.CreatedAt,
            product.UpdatedAt
        );
    }
}
