using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.StoreLayouts.Commands.Zones;
using POS.Domain.Common;
using POS.Domain.StoreLayouts;
using POS.Domain.StoreLayouts.Enums;

namespace POS.Application.UseCases.StoreLayouts.Commands.Shelves;

public class CreateShelfCommandHandler : ICommandHandler<CreateShelfCommand, ShelfDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public CreateShelfCommandHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IUnitOfWork unitOfWork,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _unitOfWork = unitOfWork;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task<Result<ShelfDto>> Handle(CreateShelfCommand request, CancellationToken cancellationToken)
    {
        var layout = await _storeLayoutRepository.GetByIdAsync(request.LayoutId, includeGraph: true, cancellationToken);
        if (layout == null)
            return Result<ShelfDto>.Failure(StoreLayoutErrors.NotFound);

        if (!_storeAccessPolicy.CanAccessStore(layout.StoreId))
            return Result<ShelfDto>.Failure(StoreLayoutErrors.StoreMismatch);

        var now = DateTime.UtcNow;
        var shelf = new Shelf(
            layout.Id,
            request.ZoneId,
            request.Code,
            request.Name,
            request.PositionX,
            request.PositionY,
            request.Width,
            request.Height,
            request.Orientation,
            now);

        // Add default level 1
        shelf.AddLevel(1, "Tầng 1", 1);

        var addResult = layout.AddShelf(shelf, request.ExpectedRevision, now);
        if (addResult.IsFailure)
            return Result<ShelfDto>.Failure(addResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ShelfDto>.Success(new ShelfDto(
            shelf.Id,
            shelf.LayoutId,
            shelf.ZoneId,
            shelf.Code,
            shelf.Name,
            shelf.PositionX,
            shelf.PositionY,
            shelf.Width,
            shelf.Height,
            shelf.Orientation,
            shelf.Status,
            shelf.Levels.Select(l => new ShelfLevelDto(l.Id, l.ShelfId, l.LevelNumber, l.Label, l.SortOrder, [])).ToList()));
    }
}

public class UpdateShelfCommandHandler : ICommandHandler<UpdateShelfCommand, ShelfDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public UpdateShelfCommandHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IUnitOfWork unitOfWork,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _unitOfWork = unitOfWork;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task<Result<ShelfDto>> Handle(UpdateShelfCommand request, CancellationToken cancellationToken)
    {
        var layout = await _storeLayoutRepository.GetByIdAsync(request.LayoutId, includeGraph: true, cancellationToken);
        if (layout == null)
            return Result<ShelfDto>.Failure(StoreLayoutErrors.NotFound);

        if (!_storeAccessPolicy.CanAccessStore(layout.StoreId))
            return Result<ShelfDto>.Failure(StoreLayoutErrors.StoreMismatch);

        var now = DateTime.UtcNow;
        var updateResult = layout.UpdateShelf(
            request.ShelfId,
            request.ZoneId,
            request.Code,
            request.Name,
            request.PositionX,
            request.PositionY,
            request.Width,
            request.Height,
            request.Orientation,
            request.Status,
            request.ExpectedRevision,
            now);

        if (updateResult.IsFailure)
            return Result<ShelfDto>.Failure(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var shelf = layout.Shelves.First(s => s.Id == request.ShelfId);
        return Result<ShelfDto>.Success(new ShelfDto(
            shelf.Id,
            shelf.LayoutId,
            shelf.ZoneId,
            shelf.Code,
            shelf.Name,
            shelf.PositionX,
            shelf.PositionY,
            shelf.Width,
            shelf.Height,
            shelf.Orientation,
            shelf.Status,
            shelf.Levels.Select(l => new ShelfLevelDto(l.Id, l.ShelfId, l.LevelNumber, l.Label, l.SortOrder,
                l.Placements.Select(p => new ProductPlacementDto(p.Id, p.ShelfLevelId, p.SkuId, p.IsPrimary, p.Note, p.UpdatedBy, p.UpdatedAt, layout.Revision)).ToList())).ToList()));
    }
}

public class DeleteShelfCommandHandler : ICommandHandler<DeleteShelfCommand, LayoutMutationResponseDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public DeleteShelfCommandHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IUnitOfWork unitOfWork,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _unitOfWork = unitOfWork;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task<Result<LayoutMutationResponseDto>> Handle(DeleteShelfCommand request, CancellationToken cancellationToken)
    {
        var layout = await _storeLayoutRepository.GetByIdAsync(request.LayoutId, includeGraph: true, cancellationToken);
        if (layout == null)
            return Result<LayoutMutationResponseDto>.Failure(StoreLayoutErrors.NotFound);

        if (!_storeAccessPolicy.CanAccessStore(layout.StoreId))
            return Result<LayoutMutationResponseDto>.Failure(StoreLayoutErrors.StoreMismatch);

        var removeResult = layout.RemoveShelf(request.ShelfId, request.ExpectedRevision, DateTime.UtcNow);
        if (removeResult.IsFailure)
            return Result<LayoutMutationResponseDto>.Failure(removeResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<LayoutMutationResponseDto>.Success(new LayoutMutationResponseDto(layout.Id, layout.Revision));
    }
}
