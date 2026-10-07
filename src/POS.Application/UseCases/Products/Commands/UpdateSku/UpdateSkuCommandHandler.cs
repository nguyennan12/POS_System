using System.Text.Json;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Caching;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Products.Commands.UpdateSku;

public class UpdateSkuCommandHandler : ICommandHandler<UpdateSkuCommand, Guid>
{
    private readonly ISkuRepository _skuRepository;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateSkuCommandHandler(
        ISkuRepository skuRepository,
        ICacheService cacheService,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _skuRepository = skuRepository;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(UpdateSkuCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.StoreId is null)
        {
            return Result<Guid>.Failure(ProductErrors.StoreRequired);
        }

        var storeId = _currentUser.StoreId.Value;

        var sku = await _skuRepository.GetByIdWithProductAsync(request.Id, cancellationToken);
        if (sku == null || sku.StoreId != storeId)
        {
            return Result<Guid>.Failure(ProductErrors.SkuNotFound(request.Id));
        }

        var allowedTaxRates = new[] { 0m, 5m, 8m, 10m };

        if (request.SellPrice < 0 || request.CostPrice < 0)
        {
            return Result<Guid>.Failure(ProductErrors.InvalidPrice);
        }

        if (!allowedTaxRates.Contains(request.TaxRate))
        {
            return Result<Guid>.Failure(ProductErrors.InvalidTaxRate);
        }

        if (!await _skuRepository.IsSkuCodeUniqueAsync(request.SkuCode, storeId, sku.Id, cancellationToken))
        {
            return Result<Guid>.Failure(ProductErrors.SkuCodeExists);
        }

        if (!await _skuRepository.IsBarcodeUniqueAsync(request.Barcode, storeId, sku.Id, cancellationToken))
        {
            return Result<Guid>.Failure(ProductErrors.BarcodeExists);
        }

        // Safe JSON Attributes parse
        Dictionary<string, string>? attributes = null;
        if (request.Attributes.HasValue)
        {
            try
            {
                attributes = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    request.Attributes.Value.GetRawText());
            }
            catch (JsonException)
            {
                attributes = null;
            }
        }

        var oldBarcode = sku.Barcode;

        sku.Update(
            request.SkuCode,
            request.Barcode,
            request.SellPrice,
            request.CostPrice,
            request.TaxRate,
            request.IsActive,
            attributes
        );

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate Redis barcode cache so the next POS scan gets fresh data
        var oldCacheKey = $"sku:barcode:{storeId}:{oldBarcode}";
        var newCacheKey = $"sku:barcode:{storeId}:{request.Barcode}";

        await _cacheService.RemoveRangeAsync(
            new[] { oldCacheKey, newCacheKey },
            cancellationToken);

        return sku.Id;
    }
}
