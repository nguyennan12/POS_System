using System.Text.Json;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Products.Queries.GetSkuById;

internal sealed class GetSkuByIdQueryHandler(
    ISkuRepository skuRepository,
    IStockEntryRepository stockEntryRepository,
    ICurrentUser currentUser) : IQueryHandler<GetSkuByIdQuery, SkuDetailDto>
{
    public async Task<Result<SkuDetailDto>> Handle(
        GetSkuByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.StoreId is null)
            return ProductErrors.StoreRequired;

        var sku = await skuRepository.GetByIdWithProductAsync(query.Id, cancellationToken);
        if (sku is null || sku.StoreId != currentUser.StoreId.Value)
            return ProductErrors.SkuNotFound(query.Id);

        // Load real stock quantity
        var stockEntry = await stockEntryRepository.GetBySkuAndStoreAsync(sku.Id, currentUser.StoreId.Value, cancellationToken);
        var qtyOnHand = stockEntry?.QtyOnHand ?? 0m;

        // UnitConversions are now eager-loaded by the repository
        var unitConversions = sku.UnitConversions?.Select(uc => new UnitConversionDto(
            uc.Id,
            uc.SkuId,
            uc.UnitName,
            uc.ConversionFactor,
            uc.SellPrice
        )).ToList() ?? new List<UnitConversionDto>();

        // Safe Attributes serialization
        JsonElement? attributes = null;
        if (sku.Attributes != null)
        {
            try
            {
                attributes = JsonSerializer.SerializeToElement(sku.Attributes);
            }
            catch (JsonException)
            {
                attributes = null;
            }
        }

        return new SkuDetailDto(
            sku.Id,
            sku.ProductId,
            sku.Product.Name,
            sku.SkuCode,
            sku.Barcode,
            attributes,
            sku.CostPrice,
            sku.SellPrice,
            sku.TaxRate,
            sku.IsActive,
            qtyOnHand,
            unitConversions,
            sku.CreatedAt,
            sku.UpdatedAt
        );
    }
}
