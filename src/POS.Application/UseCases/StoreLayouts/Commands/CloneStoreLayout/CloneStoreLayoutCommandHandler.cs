using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.StoreLayouts;

namespace POS.Application.UseCases.StoreLayouts.Commands.CloneStoreLayout;

public class CloneStoreLayoutCommandHandler : ICommandHandler<CloneStoreLayoutCommand, CloneStoreLayoutResultDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IProductPlacementRepository _productPlacementRepository;
    private readonly ISkuRepository _skuRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;
    private readonly ICurrentUser _currentUser;

    public CloneStoreLayoutCommandHandler(
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

    public async Task<Result<CloneStoreLayoutResultDto>> Handle(CloneStoreLayoutCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteSerializableAsync(async ct =>
        {
            var sourceLayout = await _storeLayoutRepository.GetByIdAsync(request.SourceLayoutId, includeGraph: true, ct);
            if (sourceLayout == null)
                return Result<CloneStoreLayoutResultDto>.Failure(StoreLayoutErrors.NotFound);

            if (!_storeAccessPolicy.CanAccessStore(sourceLayout.StoreId))
                return Result<CloneStoreLayoutResultDto>.Failure(StoreLayoutErrors.StoreMismatch);

            if (sourceLayout.Revision != request.ExpectedSourceRevision)
                return Result<CloneStoreLayoutResultDto>.Failure(StoreLayoutErrors.RevisionConflict);

            var targetStoreId = request.TargetStoreId ?? sourceLayout.StoreId;
            var targetStoreAccess = _storeAccessPolicy.EnforceStoreAccess(targetStoreId);
            if (targetStoreAccess.IsFailure)
                return Result<CloneStoreLayoutResultDto>.Failure(targetStoreAccess.Error);

            var nextVersion = await _storeLayoutRepository.GetNextVersionNumberAsync(targetStoreId, ct);
            var now = DateTime.UtcNow;
            var actorId = _currentUser.EmployeeId ?? Guid.Empty;

            var createResult = StoreLayout.Create(
                targetStoreId,
                nextVersion,
                request.NewName,
                sourceLayout.GridWidth,
                sourceLayout.GridHeight,
                actorId,
                now);

            if (createResult.IsFailure)
                return Result<CloneStoreLayoutResultDto>.Failure(createResult.Error);

            var newLayout = createResult.Value;

            // 1. Copy cells
            var cellChanges = sourceLayout.Cells
                .Select(c => (c.X, c.Y, c.CellType, c.TraversalCost))
                .ToList();
            newLayout.UpdateCells(cellChanges, newLayout.Revision, now);

            // 2. Copy zones (Map OldZoneId -> NewZoneId)
            var zoneIdMap = new Dictionary<Guid, Guid>();
            foreach (var sz in sourceLayout.Zones)
            {
                var newZoneId = Guid.NewGuid();
                zoneIdMap[sz.Id] = newZoneId;
                var newZone = new StoreZone(
                    newLayout.Id,
                    sz.Code,
                    sz.Name,
                    sz.Color,
                    sz.PositionX,
                    sz.PositionY,
                    sz.Width,
                    sz.Height,
                    now,
                    newZoneId);
                newLayout.AddZone(newZone, newLayout.Revision, now);
            }

            // 3. Copy shelves & levels (Map OldLevelId -> NewLevel)
            var levelMap = new Dictionary<Guid, ShelfLevel>();
            foreach (var ss in sourceLayout.Shelves)
            {
                var newShelfId = Guid.NewGuid();
                Guid? newZoneId = ss.ZoneId.HasValue && zoneIdMap.TryGetValue(ss.ZoneId.Value, out var zId) ? zId : null;

                var newShelf = new Shelf(
                    newLayout.Id,
                    newZoneId,
                    ss.Code,
                    ss.Name,
                    ss.PositionX,
                    ss.PositionY,
                    ss.Width,
                    ss.Height,
                    ss.Orientation,
                    now,
                    ss.Status,
                    newShelfId);

                foreach (var sl in ss.Levels)
                {
                    var newLevelId = Guid.NewGuid();
                    var newLevel = newShelf.AddLevel(sl.LevelNumber, sl.Label, sl.SortOrder, newLevelId);
                    levelMap[sl.Id] = newLevel;
                }

                newLayout.AddShelf(newShelf, newLayout.Revision, now);
            }

            _storeLayoutRepository.Add(newLayout);

            // 4. Copy placements if requested
            int mappedCount = 0;
            int unmappedCount = 0;
            var unmappedList = new List<UnmappedPlacementDto>();

            if (request.CopyPlacements)
            {
                var sourcePlacements = sourceLayout.Shelves
                    .SelectMany(s => s.Levels)
                    .SelectMany(l => l.Placements)
                    .ToList();

                // If cross-store clone, query target store SKUs for mapping
                var targetSkus = targetStoreId != sourceLayout.StoreId
                    ? await _skuRepository.GetByStoreIdAsync(targetStoreId, ct)
                    : null;

                foreach (var sp in sourcePlacements)
                {
                    if (!levelMap.TryGetValue(sp.ShelfLevelId, out var targetLevel))
                        continue;

                    Guid? targetSkuId = null;

                    if (targetStoreId == sourceLayout.StoreId)
                    {
                        targetSkuId = sp.SkuId;
                    }
                    else if (targetSkus != null)
                    {
                        var sourceSku = await _skuRepository.GetByIdAsync(sp.SkuId, ct);
                        if (sourceSku != null)
                        {
                            // Match by barcode, then by SkuCode
                            var match = (!string.IsNullOrWhiteSpace(sourceSku.Barcode)
                                ? targetSkus.FirstOrDefault(ts => ts.Barcode == sourceSku.Barcode)
                                : null)
                                ?? targetSkus.FirstOrDefault(ts => ts.SkuCode.Equals(sourceSku.SkuCode, StringComparison.OrdinalIgnoreCase));

                            if (match != null)
                            {
                                targetSkuId = match.Id;
                            }
                            else
                            {
                                unmappedCount++;
                                unmappedList.Add(new UnmappedPlacementDto(
                                    sourceSku.Id,
                                    sourceSku.SkuCode,
                                    sourceSku.Barcode,
                                    "Không tìm thấy SKU tương ứng ở cửa hàng đích (theo Barcode hoặc SkuCode)."));
                            }
                        }
                    }

                    if (targetSkuId.HasValue)
                    {
                        var newPlacement = new ProductPlacement(
                            targetLevel.Id,
                            targetSkuId.Value,
                            sp.IsPrimary,
                            sp.Note,
                            actorId,
                            now);
                        targetLevel.AddPlacement(newPlacement);
                        _productPlacementRepository.Add(newPlacement);
                        mappedCount++;
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync(ct);

            return Result<CloneStoreLayoutResultDto>.Success(new CloneStoreLayoutResultDto(
                newLayout.Id,
                newLayout.Revision,
                mappedCount,
                unmappedCount,
                unmappedList));
        }, cancellationToken);
    }
}
