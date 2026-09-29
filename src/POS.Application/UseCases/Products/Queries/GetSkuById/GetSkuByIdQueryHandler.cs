using System.Text.Json;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Products.Queries.GetSkuById;

internal sealed class GetSkuByIdQueryHandler(
    ISkuRepository skuRepository,
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

        var unitConversions = sku.UnitConversions?.Select(uc => new UnitConversionDto(
            uc.Id,
            uc.SkuId,
            uc.UnitName,
            uc.ConversionFactor,
            uc.SellPrice
        )).ToList() ?? new List<UnitConversionDto>();

        return new SkuDetailDto(
            sku.Id,
            sku.ProductId,
            sku.Product.Name,
            sku.SkuCode,
            sku.Barcode,
            sku.Attributes != null ? JsonSerializer.SerializeToElement(sku.Attributes) : null,
            sku.CostPrice,
            sku.SellPrice,
            sku.TaxRate,
            sku.IsActive,
            0, // QtyOnHand
            unitConversions,
            sku.CreatedAt,
            sku.UpdatedAt
        );
    }
}
