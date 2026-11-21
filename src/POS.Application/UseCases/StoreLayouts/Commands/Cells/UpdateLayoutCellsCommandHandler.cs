using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.StoreLayouts.Commands.Cells;

public class UpdateLayoutCellsCommandHandler : ICommandHandler<UpdateLayoutCellsCommand, StoreLayoutDetailDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public UpdateLayoutCellsCommandHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IUnitOfWork unitOfWork,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _unitOfWork = unitOfWork;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task<Result<StoreLayoutDetailDto>> Handle(UpdateLayoutCellsCommand request, CancellationToken cancellationToken)
    {
        var layout = await _storeLayoutRepository.GetByIdAsync(request.LayoutId, includeGraph: true, cancellationToken);
        if (layout == null)
            return Result<StoreLayoutDetailDto>.Failure(StoreLayoutErrors.NotFound);

        if (!_storeAccessPolicy.CanAccessStore(layout.StoreId))
            return Result<StoreLayoutDetailDto>.Failure(StoreLayoutErrors.StoreMismatch);

        // Deduplicate cell changes by (X, Y)
        var distinctChanges = request.Cells
            .GroupBy(c => (c.X, c.Y))
            .Select(g => g.Last())
            .Select(c => (c.X, c.Y, c.CellType, c.TraversalCost))
            .ToList();

        var updateResult = layout.UpdateCells(distinctChanges, request.ExpectedRevision, DateTime.UtcNow);
        if (updateResult.IsFailure)
            return Result<StoreLayoutDetailDto>.Failure(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<StoreLayoutDetailDto>.Success(StoreLayoutMapper.ToDetailDto(layout));
    }
}
