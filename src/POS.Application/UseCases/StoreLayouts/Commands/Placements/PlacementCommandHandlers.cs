using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.StoreLayouts.Commands.Zones;
using POS.Domain.Common;
using POS.Domain.StoreLayouts;
using POS.Domain.StoreLayouts.Enums;

namespace POS.Application.UseCases.StoreLayouts.Commands.Placements;

public class AssignProductPlacementCommandHandler : ICommandHandler<AssignProductPlacementCommand, ProductPlacementDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IProductPlacementRepository _productPlacementRepository;
    private readonly ISkuRepository _skuRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;
    private readonly ICurrentUser _currentUser;

    public AssignProductPlacementCommandHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IProductPlacementRepository productPlacementRepository,
        ISkuRepository skuRepository,
        IUnitOfWork unitOfWork,
        IStoreAccessPolicy storeAccessPolicy,
        ICurrentUser currentUser)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _productPlacementRepository = productPlacementRepository;
        _skuRepository = skuRepository;
        _unitOfWork = unitOfWork;
        _storeAccessPolicy = storeAccessPolicy;
        _currentUser = currentUser;
    }

    public async Task<Result<ProductPlacementDto>> Handle(AssignProductPlacementCommand request, CancellationToken cancellationToken)
    {
        var layout = await _storeLayoutRepository.GetByIdAsync(request.LayoutId, includeGraph: true, cancellationToken);
        if (layout == null)
            return Result<ProductPlacementDto>.Failure(StoreLayoutErrors.NotFound);

        if (!_storeAccessPolicy.CanAccessStore(layout.StoreId))
            return Result<ProductPlacementDto>.Failure(StoreLayoutErrors.StoreMismatch);

        if (layout.Status != StoreLayoutStatus.Draft)
            return Result<ProductPlacementDto>.Failure(StoreLayoutErrors.NotDraft);

        if (layout.Revision != request.ExpectedRevision)
            return Result<ProductPlacementDto>.Failure(StoreLayoutErrors.RevisionConflict);

        var sku = await _skuRepository.GetByIdAsync(request.SkuId, cancellationToken);
        if (sku == null || sku.StoreId != layout.StoreId || !sku.IsActive)
            return Result<ProductPlacementDto>.Failure(StoreLayoutErrors.SkuNotInStore);

        var targetLevel = layout.Shelves
            .SelectMany(s => s.Levels)
            .FirstOrDefault(l => l.Id == request.ShelfLevelId);
        if (targetLevel == null)
            return Result<ProductPlacementDto>.Failure(StoreLayoutErrors.ShelfLevelNotFound);

        if (targetLevel.Placements.Any(p => p.SkuId == request.SkuId))
            return Result<ProductPlacementDto>.Failure(new Error(ErrorType.AlreadyExists, "PLACEMENT_EXISTS_ON_LEVEL", "SKU này đã được đặt tại tầng kệ này rồi."));

        var actorId = _currentUser.EmployeeId ?? Guid.Empty;
        var now = DateTime.UtcNow;

        // If IsPrimary, unset other primary placements for this SKU on this layout
        if (request.IsPrimary)
        {
            var otherPlacements = layout.Shelves
                .SelectMany(s => s.Levels)
                .SelectMany(l => l.Placements)
                .Where(p => p.SkuId == request.SkuId && p.IsPrimary)
                .ToList();

            foreach (var op in otherPlacements)
            {
                op.SetPrimary(false, actorId, now);
            }
        }

        var placement = new ProductPlacement(
            request.ShelfLevelId,
            request.SkuId,
            request.IsPrimary,
            request.Note,
            actorId,
            now);

        targetLevel.AddPlacement(placement);
        _productPlacementRepository.Add(placement);

        var history = new ProductPlacementHistory(
            layout.Id,
            request.SkuId,
            PlacementHistoryAction.Assigned,
            null,
            request.ShelfLevelId,
            actorId,
            request.Note,
            now);
        _productPlacementRepository.AddHistory(history);

        layout.Rename(layout.Name, layout.Revision, now); // Bump revision
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ProductPlacementDto>.Success(new ProductPlacementDto(
            placement.Id,
            placement.ShelfLevelId,
            placement.SkuId,
            placement.IsPrimary,
            placement.Note,
            placement.UpdatedBy,
            placement.UpdatedAt,
            layout.Revision));
    }
}

public class MoveProductPlacementCommandHandler : ICommandHandler<MoveProductPlacementCommand, ProductPlacementDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IProductPlacementRepository _productPlacementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;
    private readonly ICurrentUser _currentUser;

    public MoveProductPlacementCommandHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IProductPlacementRepository productPlacementRepository,
        IUnitOfWork unitOfWork,
        IStoreAccessPolicy storeAccessPolicy,
        ICurrentUser currentUser)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _productPlacementRepository = productPlacementRepository;
        _unitOfWork = unitOfWork;
        _storeAccessPolicy = storeAccessPolicy;
        _currentUser = currentUser;
    }

    public async Task<Result<ProductPlacementDto>> Handle(MoveProductPlacementCommand request, CancellationToken cancellationToken)
    {
        var layout = await _storeLayoutRepository.GetByIdAsync(request.LayoutId, includeGraph: true, cancellationToken);
        if (layout == null)
            return Result<ProductPlacementDto>.Failure(StoreLayoutErrors.NotFound);

        if (!_storeAccessPolicy.CanAccessStore(layout.StoreId))
            return Result<ProductPlacementDto>.Failure(StoreLayoutErrors.StoreMismatch);

        if (layout.Status != StoreLayoutStatus.Draft)
            return Result<ProductPlacementDto>.Failure(StoreLayoutErrors.NotDraft);

        if (layout.Revision != request.ExpectedRevision)
            return Result<ProductPlacementDto>.Failure(StoreLayoutErrors.RevisionConflict);

        var placement = layout.Shelves
            .SelectMany(s => s.Levels)
            .SelectMany(l => l.Placements)
            .FirstOrDefault(p => p.Id == request.PlacementId);
        if (placement == null)
            return Result<ProductPlacementDto>.Failure(StoreLayoutErrors.ProductPlacementNotFound);

        var targetLevel = layout.Shelves
            .SelectMany(s => s.Levels)
            .FirstOrDefault(l => l.Id == request.TargetShelfLevelId);
        if (targetLevel == null)
            return Result<ProductPlacementDto>.Failure(StoreLayoutErrors.ShelfLevelNotFound);

        if (targetLevel.Id != placement.ShelfLevelId && targetLevel.Placements.Any(p => p.SkuId == placement.SkuId))
            return Result<ProductPlacementDto>.Failure(new Error(ErrorType.AlreadyExists, "PLACEMENT_EXISTS_ON_LEVEL", "SKU này đã được đặt tại tầng kệ đích rồi."));

        var actorId = _currentUser.EmployeeId ?? Guid.Empty;
        var now = DateTime.UtcNow;
        var oldLevelId = placement.ShelfLevelId;

        placement.Move(request.TargetShelfLevelId, request.Note, actorId, now);

        var history = new ProductPlacementHistory(
            layout.Id,
            placement.SkuId,
            PlacementHistoryAction.Moved,
            oldLevelId,
            request.TargetShelfLevelId,
            actorId,
            request.Note,
            now);
        _productPlacementRepository.AddHistory(history);

        layout.Rename(layout.Name, layout.Revision, now); // Bump revision
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ProductPlacementDto>.Success(new ProductPlacementDto(
            placement.Id,
            placement.ShelfLevelId,
            placement.SkuId,
            placement.IsPrimary,
            placement.Note,
            placement.UpdatedBy,
            placement.UpdatedAt,
            layout.Revision));
    }
}

public class SetPrimaryPlacementCommandHandler : ICommandHandler<SetPrimaryPlacementCommand, ProductPlacementDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IProductPlacementRepository _productPlacementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;
    private readonly ICurrentUser _currentUser;

    public SetPrimaryPlacementCommandHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IProductPlacementRepository productPlacementRepository,
        IUnitOfWork unitOfWork,
        IStoreAccessPolicy storeAccessPolicy,
        ICurrentUser currentUser)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _productPlacementRepository = productPlacementRepository;
        _unitOfWork = unitOfWork;
        _storeAccessPolicy = storeAccessPolicy;
        _currentUser = currentUser;
    }

    public async Task<Result<ProductPlacementDto>> Handle(SetPrimaryPlacementCommand request, CancellationToken cancellationToken)
    {
        var layout = await _storeLayoutRepository.GetByIdAsync(request.LayoutId, includeGraph: true, cancellationToken);
        if (layout == null)
            return Result<ProductPlacementDto>.Failure(StoreLayoutErrors.NotFound);

        if (!_storeAccessPolicy.CanAccessStore(layout.StoreId))
            return Result<ProductPlacementDto>.Failure(StoreLayoutErrors.StoreMismatch);

        if (layout.Status != StoreLayoutStatus.Draft)
            return Result<ProductPlacementDto>.Failure(StoreLayoutErrors.NotDraft);

        if (layout.Revision != request.ExpectedRevision)
            return Result<ProductPlacementDto>.Failure(StoreLayoutErrors.RevisionConflict);

        var placement = layout.Shelves
            .SelectMany(s => s.Levels)
            .SelectMany(l => l.Placements)
            .FirstOrDefault(p => p.Id == request.PlacementId);
        if (placement == null)
            return Result<ProductPlacementDto>.Failure(StoreLayoutErrors.ProductPlacementNotFound);

        var actorId = _currentUser.EmployeeId ?? Guid.Empty;
        var now = DateTime.UtcNow;

        // Unset any other primary placement for this SKU
        var otherPrimaries = layout.Shelves
            .SelectMany(s => s.Levels)
            .SelectMany(l => l.Placements)
            .Where(p => p.SkuId == placement.SkuId && p.Id != placement.Id && p.IsPrimary)
            .ToList();

        foreach (var op in otherPrimaries)
        {
            op.SetPrimary(false, actorId, now);
        }

        placement.SetPrimary(true, actorId, now);

        var history = new ProductPlacementHistory(
            layout.Id,
            placement.SkuId,
            PlacementHistoryAction.PrimaryChanged,
            placement.ShelfLevelId,
            placement.ShelfLevelId,
            actorId,
            "Đã đặt làm vị trí chính (Primary)",
            now);
        _productPlacementRepository.AddHistory(history);

        layout.Rename(layout.Name, layout.Revision, now); // Bump revision
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ProductPlacementDto>.Success(new ProductPlacementDto(
            placement.Id,
            placement.ShelfLevelId,
            placement.SkuId,
            placement.IsPrimary,
            placement.Note,
            placement.UpdatedBy,
            placement.UpdatedAt,
            layout.Revision));
    }
}

public class RemoveProductPlacementCommandHandler : ICommandHandler<RemoveProductPlacementCommand, LayoutMutationResponseDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IProductPlacementRepository _productPlacementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;
    private readonly ICurrentUser _currentUser;

    public RemoveProductPlacementCommandHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IProductPlacementRepository productPlacementRepository,
        IUnitOfWork unitOfWork,
        IStoreAccessPolicy storeAccessPolicy,
        ICurrentUser currentUser)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _productPlacementRepository = productPlacementRepository;
        _unitOfWork = unitOfWork;
        _storeAccessPolicy = storeAccessPolicy;
        _currentUser = currentUser;
    }

    public async Task<Result<LayoutMutationResponseDto>> Handle(RemoveProductPlacementCommand request, CancellationToken cancellationToken)
    {
        var layout = await _storeLayoutRepository.GetByIdAsync(request.LayoutId, includeGraph: true, cancellationToken);
        if (layout == null)
            return Result<LayoutMutationResponseDto>.Failure(StoreLayoutErrors.NotFound);

        if (!_storeAccessPolicy.CanAccessStore(layout.StoreId))
            return Result<LayoutMutationResponseDto>.Failure(StoreLayoutErrors.StoreMismatch);

        if (layout.Status != StoreLayoutStatus.Draft)
            return Result<LayoutMutationResponseDto>.Failure(StoreLayoutErrors.NotDraft);

        if (layout.Revision != request.ExpectedRevision)
            return Result<LayoutMutationResponseDto>.Failure(StoreLayoutErrors.RevisionConflict);

        var targetLevel = layout.Shelves
            .SelectMany(s => s.Levels)
            .FirstOrDefault(l => l.Placements.Any(p => p.Id == request.PlacementId));
        if (targetLevel == null)
            return Result<LayoutMutationResponseDto>.Failure(StoreLayoutErrors.ProductPlacementNotFound);

        var placement = targetLevel.Placements.First(p => p.Id == request.PlacementId);
        var actorId = _currentUser.EmployeeId ?? Guid.Empty;
        var now = DateTime.UtcNow;

        targetLevel.RemovePlacement(placement);
        _productPlacementRepository.Remove(placement);

        var history = new ProductPlacementHistory(
            layout.Id,
            placement.SkuId,
            PlacementHistoryAction.Removed,
            placement.ShelfLevelId,
            null,
            actorId,
            request.Note,
            now);
        _productPlacementRepository.AddHistory(history);

        layout.Rename(layout.Name, layout.Revision, now); // Bump revision
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LayoutMutationResponseDto>.Success(new LayoutMutationResponseDto(layout.Id, layout.Revision));
    }
}
