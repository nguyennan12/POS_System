using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.StoreLayouts.Commands.UpdateStoreLayout;

public class UpdateStoreLayoutCommandHandler : ICommandHandler<UpdateStoreLayoutCommand, StoreLayoutDetailDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public UpdateStoreLayoutCommandHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IUnitOfWork unitOfWork,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _unitOfWork = unitOfWork;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task<Result<StoreLayoutDetailDto>> Handle(UpdateStoreLayoutCommand request, CancellationToken cancellationToken)
    {
        var layout = await _storeLayoutRepository.GetForUpdateAsync(request.LayoutId, cancellationToken);
        if (layout == null)
            return Result<StoreLayoutDetailDto>.Failure(StoreLayoutErrors.NotFound);

        if (!_storeAccessPolicy.CanAccessStore(layout.StoreId))
            return Result<StoreLayoutDetailDto>.Failure(StoreLayoutErrors.StoreMismatch);

        var renameResult = layout.Rename(request.Name, request.ExpectedRevision, DateTime.UtcNow);
        if (renameResult.IsFailure)
            return Result<StoreLayoutDetailDto>.Failure(renameResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with graph to return full detail
        var fullLayout = await _storeLayoutRepository.GetByIdAsync(layout.Id, includeGraph: true, cancellationToken);
        return Result<StoreLayoutDetailDto>.Success(StoreLayoutMapper.ToDetailDto(fullLayout ?? layout));
    }
}
