using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Products.Enums;

namespace POS.Application.UseCases.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, Guid>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateProductCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.StoreId is null)
        {
            return Result<Guid>.Failure(ProductErrors.StoreRequired);
        }

        var storeId = _currentUser.StoreId.Value;

        var product = await _productRepository.GetByIdWithSkusAsync(request.Id, storeId, cancellationToken);
        if (product == null)
        {
            return Result<Guid>.Failure(ProductErrors.ProductNotFound(request.Id));
        }

        if (!Enum.TryParse<ProductStatus>(request.Status, true, out var parsedStatus))
        {
            parsedStatus = ProductStatus.Active;
        }

        product.Update(
            request.CategoryId,
            request.Name,
            request.BaseUnit,
            request.Description,
            request.Brand,
            request.ImageUrl,
            parsedStatus
        );

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
