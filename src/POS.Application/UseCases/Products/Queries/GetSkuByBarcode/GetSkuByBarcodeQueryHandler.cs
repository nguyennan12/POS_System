using System.Text.Json;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Products.Queries.GetSkuByBarcode;

internal sealed class GetSkuByBarcodeQueryHandler(
    ISkuRepository skuRepository,
    ICurrentUser currentUser) : IQueryHandler<GetSkuByBarcodeQuery, SkuBarcodeLookupDto>
{
    public async Task<Result<SkuBarcodeLookupDto>> Handle(
        GetSkuByBarcodeQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.StoreId is null)
            return ProductErrors.StoreRequired;

        var sku = await skuRepository.GetByBarcodeWithProductAsync(query.Barcode, currentUser.StoreId.Value, cancellationToken);
        if (sku is null)
            return ProductErrors.BarcodeNotFound;

        var unitConversions = sku.UnitConversions?.Select(uc => new UnitConversionDto(
            uc.Id,
            uc.SkuId,
            uc.UnitName,
            uc.ConversionFactor,
            uc.SellPrice
        )).ToList() ?? new List<UnitConversionDto>();

        return new SkuBarcodeLookupDto(
            sku.Id,
            sku.ProductId,
            sku.Product.Name,
            sku.SkuCode,
            sku.Barcode,
            sku.SellPrice,
            sku.TaxRate,
            0, // StockAvailable - assuming 0 for now as stock might need a separate service
            sku.Product.BaseUnit,
            unitConversions
        );
    }
}
