using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Products.Commands.DeleteSku;

public class DeleteSkuCommandHandler : ICommandHandler<DeleteSkuCommand>
{
    private readonly ISkuRepository _skuRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteSkuCommandHandler(
        ISkuRepository skuRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _skuRepository = skuRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteSkuCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.StoreId is null)
            return Result.Failure(ProductErrors.StoreRequired);

        var sku = await _skuRepository.GetByIdWithProductAsync(request.SkuId, cancellationToken);
        if (sku is null)
            return Result.Failure(ProductErrors.SkuNotFound(request.SkuId));

        // Only allow deletion of SKUs belonging to the current store
        if (sku.StoreId != _currentUser.StoreId.Value)
            return Result.Failure(ProductErrors.SkuNotFound(request.SkuId));

        _skuRepository.Remove(sku);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
