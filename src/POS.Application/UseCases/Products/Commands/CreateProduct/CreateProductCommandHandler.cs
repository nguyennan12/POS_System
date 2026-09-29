using System.Text.Json;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Products;

namespace POS.Application.UseCases.Products.Commands.CreateProduct;

public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _productRepository;
    private readonly ISkuRepository _skuRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        ISkuRepository skuRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _productRepository = productRepository;
        _skuRepository = skuRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.StoreId is null)
        {
            return Result<Guid>.Failure(ProductErrors.StoreRequired);
        }

        var storeId = _currentUser.StoreId.Value;

        var product = new Product(
            storeId,
            request.CategoryId,
            request.Name,
            request.BaseUnit,
            request.Description,
            request.Brand,
            request.ImageUrl
        );

        await _productRepository.AddAsync(product, cancellationToken);

        if (request.Skus != null && request.Skus.Any())
        {
            var allowedTaxRates = new[] { 0m, 5m, 8m, 10m };

            foreach (var skuInfo in request.Skus)
            {
                if (skuInfo.SellPrice < 0 || skuInfo.CostPrice < 0)
                {
                    return Result<Guid>.Failure(ProductErrors.InvalidPrice);
                }

                if (!allowedTaxRates.Contains(skuInfo.TaxRate))
                {
                    return Result<Guid>.Failure(ProductErrors.InvalidTaxRate);
                }

                if (!await _skuRepository.IsSkuCodeUniqueAsync(skuInfo.SkuCode, storeId, null, cancellationToken))
                {
                    return Result<Guid>.Failure(ProductErrors.SkuCodeExists);
                }

                if (!await _skuRepository.IsBarcodeUniqueAsync(skuInfo.Barcode, storeId, null, cancellationToken))
                {
                    return Result<Guid>.Failure(ProductErrors.BarcodeExists);
                }

                var attributes = skuInfo.Attributes.HasValue 
                    ? JsonSerializer.Deserialize<Dictionary<string, string>>(skuInfo.Attributes.Value.GetRawText())
                    : null;

                var sku = new Sku(
                    product.Id,
                    storeId,
                    skuInfo.SkuCode,
                    skuInfo.Barcode,
                    skuInfo.SellPrice,
                    skuInfo.CostPrice,
                    skuInfo.TaxRate,
                    true,
                    attributes
                );

                await _skuRepository.AddAsync(sku, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
