using POS.Application.Abstractions.Persistence;
using POS.Application.Abstractions.StoreLayouts;
using POS.Domain.Common;
using POS.Domain.StoreLayouts;
using POS.Domain.StoreLayouts.Enums;
using POS.Domain.StoreLayouts.Errors;

namespace POS.Application.UseCases.StoreLayouts.Services;

public class StoreLayoutValidationService : IStoreLayoutValidationService
{
    private readonly ISkuRepository _skuRepository;
    private readonly IPathFindingService _pathFindingService;

    public StoreLayoutValidationService(
        ISkuRepository skuRepository,
        IPathFindingService pathFindingService)
    {
        _skuRepository = skuRepository;
        _pathFindingService = pathFindingService;
    }

    public async Task<Result> ValidateForPublishAsync(
        StoreLayout layout,
        Guid expectedStoreId,
        CancellationToken cancellationToken = default)
    {
        if (layout.StoreId != expectedStoreId)
            return Result.Failure(StoreLayoutErrors.StoreMismatch);

        if (layout.Status != StoreLayoutStatus.Draft)
            return Result.Failure(StoreLayoutErrors.NotDraft);

        if (layout.GridWidth < 4 || layout.GridWidth > 100 || layout.GridHeight < 4 || layout.GridHeight > 100)
            return Result.Failure(StoreLayoutErrors.InvalidGrid);

        var entranceCell = layout.Cells.FirstOrDefault(c => c.CellType == LayoutCellType.Entrance);
        if (entranceCell == null)
            return Result.Failure(StoreLayoutErrors.EntranceRequired);

        // Check shelves footprint & overlaps
        var shelvesList = layout.Shelves.ToList();
        for (int i = 0; i < shelvesList.Count; i++)
        {
            var s1 = shelvesList[i];
            if (s1.PositionX < 0 || s1.PositionY < 0 ||
                s1.PositionX + s1.Width > layout.GridWidth ||
                s1.PositionY + s1.Height > layout.GridHeight)
            {
                return Result.Failure(StoreLayoutErrors.ShelfOutOfBounds);
            }

            // Check overlap with other shelves
            for (int j = i + 1; j < shelvesList.Count; j++)
            {
                var s2 = shelvesList[j];
                if (s1.PositionX < s2.PositionX + s2.Width &&
                    s1.PositionX + s1.Width > s2.PositionX &&
                    s1.PositionY < s2.PositionY + s2.Height &&
                    s1.PositionY + s1.Height > s2.PositionY)
                {
                    return Result.Failure(StoreLayoutErrors.ShelfOverlap);
                }
            }

            // Check if shelf sits on entrance/checkout/blocked
            bool onForbidden = layout.Cells.Any(c =>
                c.X >= s1.PositionX && c.X < s1.PositionX + s1.Width &&
                c.Y >= s1.PositionY && c.Y < s1.PositionY + s1.Height &&
                (c.CellType == LayoutCellType.Entrance || c.CellType == LayoutCellType.Checkout || c.CellType == LayoutCellType.Blocked));

            if (onForbidden)
                return Result.Failure(StoreLayoutErrors.ShelfCellBlocked);
        }

        // Check placement SKU belongs to store and at most one primary
        var allPlacements = layout.Shelves
            .SelectMany(s => s.Levels)
            .SelectMany(l => l.Placements)
            .ToList();

        var placementsBySku = allPlacements.GroupBy(p => p.SkuId);
        foreach (var group in placementsBySku)
        {
            if (group.Count(p => p.IsPrimary) > 1)
                return Result.Failure(StoreLayoutErrors.PrimaryConflict);

            var sku = await _skuRepository.GetByIdAsync(group.Key, cancellationToken);
            if (sku == null || sku.StoreId != expectedStoreId || !sku.IsActive)
                return Result.Failure(StoreLayoutErrors.SkuNotInStore);
        }

        // Path validation: each shelf must have at least one reachable adjacent cell from entrance
        var unwalkable = new HashSet<(int X, int Y)>();
        foreach (var c in layout.Cells.Where(c => c.CellType == LayoutCellType.Blocked))
        {
            unwalkable.Add((c.X, c.Y));
        }
        foreach (var s in layout.Shelves)
        {
            for (int y = s.PositionY; y < s.PositionY + s.Height; y++)
            {
                for (int x = s.PositionX; x < s.PositionX + s.Width; x++)
                {
                    unwalkable.Add((x, y));
                }
            }
        }

        var startPoint = new LayoutPointDto(entranceCell.X, entranceCell.Y);
        foreach (var shelf in layout.Shelves.Where(s => s.Status == ShelfStatus.Active))
        {
            var adjCells = GetAdjacentWalkable(layout.GridWidth, layout.GridHeight, shelf.PositionX, shelf.PositionY, shelf.Width, shelf.Height, unwalkable);
            if (adjCells.Count == 0)
                return Result.Failure(StoreLayoutErrors.UnreachableShelf);

            bool reachable = adjCells.Any(adj =>
                _pathFindingService.FindShortestPath(layout.GridWidth, layout.GridHeight, unwalkable, startPoint, adj) != null);

            if (!reachable)
                return Result.Failure(StoreLayoutErrors.UnreachableShelf);
        }

        return Result.Success();
    }

    private static List<LayoutPointDto> GetAdjacentWalkable(
        int gridWidth,
        int gridHeight,
        int sx,
        int sy,
        int sw,
        int sh,
        HashSet<(int X, int Y)> unwalkable)
    {
        var list = new List<LayoutPointDto>();
        // Top & bottom
        for (int x = sx; x < sx + sw; x++)
        {
            if (sy - 1 >= 0 && !unwalkable.Contains((x, sy - 1))) list.Add(new LayoutPointDto(x, sy - 1));
            if (sy + sh < gridHeight && !unwalkable.Contains((x, sy + sh))) list.Add(new LayoutPointDto(x, sy + sh));
        }
        // Left & right
        for (int y = sy; y < sy + sh; y++)
        {
            if (sx - 1 >= 0 && !unwalkable.Contains((sx - 1, y))) list.Add(new LayoutPointDto(sx - 1, y));
            if (sx + sw < gridWidth && !unwalkable.Contains((sx + sw, y))) list.Add(new LayoutPointDto(sx + sw, y));
        }
        return list;
    }
}
