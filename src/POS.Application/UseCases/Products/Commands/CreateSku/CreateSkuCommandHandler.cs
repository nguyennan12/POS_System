using System.Text.Json;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Products;

namespace POS.Application.UseCases.Products.Commands.CreateSku;

public class CreateSkuCommandHandler : ICommandHandler<CreateSkuCommand, Guid>
{
    private readonly ISkuRepository _skuRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateSkuCommandHandler(
        ISkuRepository skuRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _skuRepository = skuRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateSkuCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.StoreId is null)
        {
            return Result<Guid>.Failure(ProductErrors.StoreRequired);
        }

        var storeId = _currentUser.StoreId.Value;

        var product = await _productRepository.GetByIdWithSkusAsync(request.ProductId, storeId, cancellationToken);
        if (product == null)
        {
            return Result<Guid>.Failure(ProductErrors.ProductNotFound(request.ProductId));
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

        if (!await _skuRepository.IsSkuCodeUniqueAsync(request.SkuCode, storeId, null, cancellationToken))
        {
            return Result<Guid>.Failure(ProductErrors.SkuCodeExists);
        }

        if (!await _skuRepository.IsBarcodeUniqueAsync(request.Barcode, storeId, null, cancellationToken))
        {
            return Result<Guid>.Failure(ProductErrors.BarcodeExists);
        }

        var attributes = request.Attributes.HasValue 
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(request.Attributes.Value.GetRawText())
            : null;

        var sku = new Sku(
            product.Id,
            storeId,
            request.SkuCode,
            request.Barcode,
            request.SellPrice,
            request.CostPrice,
            request.TaxRate,
            true,
            attributes
        );

        await _skuRepository.AddAsync(sku, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return sku.Id;
    }
}
