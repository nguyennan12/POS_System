using System.Text.Json;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Products.Enums;

namespace POS.Application.UseCases.Products.Queries.GetSkuByBarcode;

internal sealed class GetSkuByBarcodeQueryHandler(
    ISkuRepository skuRepository,
    IStockEntryRepository stockEntryRepository,
    ICacheService cacheService,
    ICurrentUser currentUser) : IQueryHandler<GetSkuByBarcodeQuery, SkuBarcodeLookupDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public async Task<Result<SkuBarcodeLookupDto>> Handle(
        GetSkuByBarcodeQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.StoreId is null)
            return ProductErrors.StoreRequired;

        var storeId = currentUser.StoreId.Value;

        // Redis cache key scoped to store + barcode for per-store correctness
        var cacheKey = $"sku:barcode:{storeId}:{query.Barcode}";

        var cached = await cacheService.GetAsync<SkuBarcodeLookupDto>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var sku = await skuRepository.GetByBarcodeWithProductAsync(query.Barcode, storeId, cancellationToken);
        if (sku is null)
            return ProductErrors.BarcodeNotFound;

        // POS-level active status checks
        if (!sku.IsActive)
            return ProductErrors.SkuInactive(sku.SkuCode);

        if (sku.Product.Status != ProductStatus.Active)
            return ProductErrors.ProductInactive(sku.Product.Name);

        // Load real stock from StockEntry
        var stockEntry = await stockEntryRepository.GetBySkuAndStoreAsync(sku.Id, storeId, cancellationToken);
        var qtyOnHand = stockEntry?.QtyOnHand ?? 0m;

        var unitConversions = sku.UnitConversions?.Select(uc => new UnitConversionDto(
            uc.Id,
            uc.SkuId,
            uc.UnitName,
            uc.ConversionFactor,
            uc.SellPrice
        )).ToList() ?? new List<UnitConversionDto>();

        var dto = new SkuBarcodeLookupDto(
            sku.Id,
            sku.ProductId,
            sku.Product.Name,
            sku.SkuCode,
            sku.Barcode,
            sku.SellPrice,
            sku.TaxRate,
            qtyOnHand,
            sku.Product.BaseUnit,
            unitConversions
        );

        // Cache the result for fast subsequent barcode scans
        await cacheService.SetAsync(cacheKey, dto, CacheTtl, cancellationToken);

        return dto;
    }
}
