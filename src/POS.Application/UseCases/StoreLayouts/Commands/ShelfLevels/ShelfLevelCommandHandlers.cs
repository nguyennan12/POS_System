using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.StoreLayouts.Commands.Zones;
using POS.Domain.Common;

namespace POS.Application.UseCases.StoreLayouts.Commands.ShelfLevels;

public class AddShelfLevelCommandHandler : ICommandHandler<AddShelfLevelCommand, ShelfLevelDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public AddShelfLevelCommandHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IUnitOfWork unitOfWork,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _unitOfWork = unitOfWork;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task<Result<ShelfLevelDto>> Handle(AddShelfLevelCommand request, CancellationToken cancellationToken)
    {
        var layout = await _storeLayoutRepository.GetByIdAsync(request.LayoutId, includeGraph: true, cancellationToken);
        if (layout == null)
            return Result<ShelfLevelDto>.Failure(StoreLayoutErrors.NotFound);

        if (!_storeAccessPolicy.CanAccessStore(layout.StoreId))
            return Result<ShelfLevelDto>.Failure(StoreLayoutErrors.StoreMismatch);

        if (layout.Status != POS.Domain.StoreLayouts.Enums.StoreLayoutStatus.Draft)
            return Result<ShelfLevelDto>.Failure(StoreLayoutErrors.NotDraft);

        if (layout.Revision != request.ExpectedRevision)
            return Result<ShelfLevelDto>.Failure(StoreLayoutErrors.RevisionConflict);

        var shelf = layout.Shelves.FirstOrDefault(s => s.Id == request.ShelfId);
        if (shelf == null)
            return Result<ShelfLevelDto>.Failure(StoreLayoutErrors.ShelfNotFound);

        if (shelf.Levels.Any(l => l.LevelNumber == request.LevelNumber))
            return Result<ShelfLevelDto>.Failure(new Error(ErrorType.AlreadyExists, "LEVEL_NUMBER_DUPLICATE", $"Tầng {request.LevelNumber} đã tồn tại trên kệ này."));

        var level = shelf.AddLevel(request.LevelNumber, request.Label, request.SortOrder);
        layout.Rename(layout.Name, layout.Revision, DateTime.UtcNow); // Bump revision

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ShelfLevelDto>.Success(new ShelfLevelDto(
            level.Id,
            level.ShelfId,
            level.LevelNumber,
            level.Label,
            level.SortOrder,
            []));
    }
}

public class UpdateShelfLevelCommandHandler : ICommandHandler<UpdateShelfLevelCommand, ShelfLevelDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public UpdateShelfLevelCommandHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IUnitOfWork unitOfWork,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _unitOfWork = unitOfWork;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task<Result<ShelfLevelDto>> Handle(UpdateShelfLevelCommand request, CancellationToken cancellationToken)
    {
        var layout = await _storeLayoutRepository.GetByIdAsync(request.LayoutId, includeGraph: true, cancellationToken);
        if (layout == null)
            return Result<ShelfLevelDto>.Failure(StoreLayoutErrors.NotFound);

        if (!_storeAccessPolicy.CanAccessStore(layout.StoreId))
            return Result<ShelfLevelDto>.Failure(StoreLayoutErrors.StoreMismatch);

        if (layout.Status != POS.Domain.StoreLayouts.Enums.StoreLayoutStatus.Draft)
            return Result<ShelfLevelDto>.Failure(StoreLayoutErrors.NotDraft);

        if (layout.Revision != request.ExpectedRevision)
            return Result<ShelfLevelDto>.Failure(StoreLayoutErrors.RevisionConflict);

        var shelf = layout.Shelves.FirstOrDefault(s => s.Id == request.ShelfId);
        if (shelf == null)
            return Result<ShelfLevelDto>.Failure(StoreLayoutErrors.ShelfNotFound);

        var level = shelf.Levels.FirstOrDefault(l => l.Id == request.ShelfLevelId);
        if (level == null)
            return Result<ShelfLevelDto>.Failure(StoreLayoutErrors.ShelfLevelNotFound);

        level.Update(request.Label, request.SortOrder);
        layout.Rename(layout.Name, layout.Revision, DateTime.UtcNow); // Bump revision

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ShelfLevelDto>.Success(new ShelfLevelDto(
            level.Id,
            level.ShelfId,
            level.LevelNumber,
            level.Label,
            level.SortOrder,
            level.Placements.Select(p => new ProductPlacementDto(p.Id, p.ShelfLevelId, p.SkuId, p.IsPrimary, p.Note, p.UpdatedBy, p.UpdatedAt, layout.Revision)).ToList()));
    }
}

public class DeleteShelfLevelCommandHandler : ICommandHandler<DeleteShelfLevelCommand, LayoutMutationResponseDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public DeleteShelfLevelCommandHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IUnitOfWork unitOfWork,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _unitOfWork = unitOfWork;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task<Result<LayoutMutationResponseDto>> Handle(DeleteShelfLevelCommand request, CancellationToken cancellationToken)
    {
        var layout = await _storeLayoutRepository.GetByIdAsync(request.LayoutId, includeGraph: true, cancellationToken);
        if (layout == null)
            return Result<LayoutMutationResponseDto>.Failure(StoreLayoutErrors.NotFound);

        if (!_storeAccessPolicy.CanAccessStore(layout.StoreId))
            return Result<LayoutMutationResponseDto>.Failure(StoreLayoutErrors.StoreMismatch);

        if (layout.Status != POS.Domain.StoreLayouts.Enums.StoreLayoutStatus.Draft)
            return Result<LayoutMutationResponseDto>.Failure(StoreLayoutErrors.NotDraft);

        if (layout.Revision != request.ExpectedRevision)
            return Result<LayoutMutationResponseDto>.Failure(StoreLayoutErrors.RevisionConflict);

        var shelf = layout.Shelves.FirstOrDefault(s => s.Id == request.ShelfId);
        if (shelf == null)
            return Result<LayoutMutationResponseDto>.Failure(StoreLayoutErrors.ShelfNotFound);

        var level = shelf.Levels.FirstOrDefault(l => l.Id == request.ShelfLevelId);
        if (level == null)
            return Result<LayoutMutationResponseDto>.Failure(StoreLayoutErrors.ShelfLevelNotFound);

        if (level.Placements.Count > 0)
            return Result<LayoutMutationResponseDto>.Failure(StoreLayoutErrors.ShelfHasPlacements);

        shelf.RemoveLevel(level.Id);
        layout.Rename(layout.Name, layout.Revision, DateTime.UtcNow); // Bump revision

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<LayoutMutationResponseDto>.Success(new LayoutMutationResponseDto(layout.Id, layout.Revision));
    }
}
