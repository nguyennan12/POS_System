using System.Text.Json;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Products.Queries.GetProductById;

internal sealed class GetProductByIdQueryHandler(
    IProductRepository productRepository,
    IStockEntryRepository stockEntryRepository,
    ICurrentUser currentUser) : IQueryHandler<GetProductByIdQuery, ProductDetailDto>
{
    public async Task<Result<ProductDetailDto>> Handle(
        GetProductByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.StoreId is null)
            return ProductErrors.StoreRequired;

        var storeId = currentUser.StoreId.Value;

        var product = await productRepository.GetByIdWithSkusAsync(query.Id, storeId, cancellationToken);
        if (product is null)
            return ProductErrors.ProductNotFound(query.Id);

        // Load all SKU stock entries in parallel for this product's store
        var skuIds = product.Skus?.Select(s => s.Id).ToList() ?? new List<Guid>();

        // Build per-SKU stock map
        var stockMap = new Dictionary<Guid, decimal>();
        foreach (var skuId in skuIds)
        {
            var stockEntry = await stockEntryRepository.GetBySkuAndStoreAsync(skuId, storeId, cancellationToken);
            stockMap[skuId] = stockEntry?.QtyOnHand ?? 0m;
        }

        var skuDtos = product.Skus?.Select(s =>
        {
            // Safe JSON serialization of Attributes
            JsonElement? attributes = null;
            if (s.Attributes != null)
            {
                try { attributes = JsonSerializer.SerializeToElement(s.Attributes); }
                catch (JsonException) { attributes = null; }
            }

            var unitConversions = s.UnitConversions?.Select(uc => new UnitConversionDto(
                uc.Id,
                uc.SkuId,
                uc.UnitName,
                uc.ConversionFactor,
                uc.SellPrice
            )).ToList() ?? new List<UnitConversionDto>();

            return new SkuDetailDto(
                s.Id,
                s.ProductId,
                product.Name,
                s.SkuCode,
                s.Barcode,
                attributes,
                s.CostPrice,
                s.SellPrice,
                s.TaxRate,
                s.IsActive,
                stockMap.GetValueOrDefault(s.Id, 0m),
                unitConversions,
                s.CreatedAt,
                s.UpdatedAt
            );
        }).ToList() ?? new List<SkuDetailDto>();

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
