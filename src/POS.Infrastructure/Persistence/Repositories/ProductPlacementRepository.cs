using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.StoreLayouts;
using POS.Domain.StoreLayouts;
using POS.Domain.StoreLayouts.Enums;

namespace POS.Infrastructure.Persistence.Repositories;

public class ProductPlacementRepository(AppDbContext dbContext) : IProductPlacementRepository
{
    public async Task<ProductPlacement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.ProductPlacements
            .Include(p => p.ShelfLevel)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<ProductLocationsDto> GetActiveLocationsAsync(
        Guid storeId,
        Guid skuId,
        CancellationToken cancellationToken = default)
    {
        var activeLayout = await dbContext.StoreLayouts
            .AsNoTracking()
            .Include(l => l.Zones)
            .Include(l => l.Cells)
            .Include(l => l.Shelves)
                .ThenInclude(s => s.Levels)
                    .ThenInclude(lv => lv.Placements)
            .FirstOrDefaultAsync(l => l.StoreId == storeId && l.Status == StoreLayoutStatus.Active, cancellationToken);

        if (activeLayout == null)
        {
            return new ProductLocationsDto(skuId, null, null, false, []);
        }

        var locations = new List<ProductLocationDto>();

        foreach (var shelf in activeLayout.Shelves.Where(s => s.Status == ShelfStatus.Active))
        {
            var zone = shelf.ZoneId.HasValue
                ? activeLayout.Zones.FirstOrDefault(z => z.Id == shelf.ZoneId.Value)
                : null;
            var zoneId = zone?.Id ?? Guid.Empty;
            var zoneName = zone?.Name ?? "Khu vực chung";

            // Default pick cell to adjacent cell or shelf front
            int pickX = Math.Max(0, shelf.PositionX);
            int pickY = Math.Max(0, shelf.PositionY + shelf.Height);
            if (pickY >= activeLayout.GridHeight) pickY = Math.Max(0, shelf.PositionY - 1);

            foreach (var level in shelf.Levels)
            {
                var placement = level.Placements.FirstOrDefault(p => p.SkuId == skuId);
                if (placement != null)
                {
                    locations.Add(new ProductLocationDto(
                        zoneId,
                        zoneName,
                        shelf.Id,
                        shelf.Code,
                        shelf.Name,
                        level.Id,
                        level.LevelNumber,
                        placement.IsPrimary,
                        pickX,
                        pickY));
                }
            }
        }

        var sortedLocations = locations
            .OrderByDescending(l => l.IsPrimary)
            .ThenBy(l => l.ShelfCode)
            .ThenBy(l => l.LevelNumber)
            .ToList();

        return new ProductLocationsDto(
            skuId,
            activeLayout.Id,
            activeLayout.VersionNumber,
            sortedLocations.Count > 0,
            sortedLocations);
    }

    public async Task<ProductLocationsDto> GetLocationsByLayoutAsync(
        Guid layoutId,
        Guid skuId,
        CancellationToken cancellationToken = default)
    {
        var targetLayout = await dbContext.StoreLayouts
            .AsNoTracking()
            .Include(l => l.Zones)
            .Include(l => l.Cells)
            .Include(l => l.Shelves)
                .ThenInclude(s => s.Levels)
                    .ThenInclude(lv => lv.Placements)
            .FirstOrDefaultAsync(l => l.Id == layoutId, cancellationToken);

        if (targetLayout == null)
        {
            return new ProductLocationsDto(skuId, null, null, false, []);
        }

        var locations = new List<ProductLocationDto>();

        foreach (var shelf in targetLayout.Shelves.Where(s => s.Status == ShelfStatus.Active))
        {
            var zone = shelf.ZoneId.HasValue
                ? targetLayout.Zones.FirstOrDefault(z => z.Id == shelf.ZoneId.Value)
                : null;
            var zoneId = zone?.Id ?? Guid.Empty;
            var zoneName = zone?.Name ?? "Khu vực chung";

            int pickX = Math.Max(0, shelf.PositionX);
            int pickY = Math.Max(0, shelf.PositionY + shelf.Height);
            if (pickY >= targetLayout.GridHeight) pickY = Math.Max(0, shelf.PositionY - 1);

            foreach (var level in shelf.Levels)
            {
                var placement = level.Placements.FirstOrDefault(p => p.SkuId == skuId);
                if (placement != null)
                {
                    locations.Add(new ProductLocationDto(
                        zoneId,
                        zoneName,
                        shelf.Id,
                        shelf.Code,
                        shelf.Name,
                        level.Id,
                        level.LevelNumber,
                        placement.IsPrimary,
                        pickX,
                        pickY));
                }
            }
        }

        var sortedLocations = locations
            .OrderByDescending(l => l.IsPrimary)
            .ThenBy(l => l.ShelfCode)
            .ThenBy(l => l.LevelNumber)
            .ToList();

        return new ProductLocationsDto(
            skuId,
            targetLayout.Id,
            targetLayout.VersionNumber,
            sortedLocations.Count > 0,
            sortedLocations);
    }

    public async Task<ShelfContentsDto?> GetShelfContentsAsync(
        Guid storeId,
        Guid shelfId,
        CancellationToken cancellationToken = default)
    {
        var shelf = await dbContext.Shelves
            .AsNoTracking()
            .Include(s => s.Layout)
            .Include(s => s.Levels)
                .ThenInclude(l => l.Placements)
            .FirstOrDefaultAsync(s => s.Id == shelfId && s.Layout != null && s.Layout.StoreId == storeId, cancellationToken);

        if (shelf == null) return null;

        var allSkuIds = shelf.Levels
            .SelectMany(l => l.Placements)
            .Select(p => p.SkuId)
            .Distinct()
            .ToList();

        var skus = await dbContext.Skus
            .AsNoTracking()
            .Include(s => s.Product)
            .Where(s => allSkuIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var levelContents = new List<ShelfLevelContentsDto>();

        foreach (var level in shelf.Levels.OrderBy(l => l.SortOrder).ThenBy(l => l.LevelNumber))
        {
            var skuPlacements = new List<ShelfSkuContentDto>();

            foreach (var p in level.Placements.OrderByDescending(p => p.IsPrimary))
            {
                skus.TryGetValue(p.SkuId, out var sku);
                var skuCode = sku?.SkuCode ?? "UNKNOWN";
                var productName = sku?.Product?.Name ?? skuCode;
                var barcode = sku?.Barcode;

                skuPlacements.Add(new ShelfSkuContentDto(
                    p.Id,
                    p.SkuId,
                    skuCode,
                    productName,
                    barcode,
                    p.IsPrimary,
                    p.Note));
            }

            levelContents.Add(new ShelfLevelContentsDto(
                level.Id,
                level.LevelNumber,
                level.Label,
                skuPlacements));
        }

        return new ShelfContentsDto(shelf.Id, shelf.Code, shelf.Name, levelContents);
    }

    public async Task<(IReadOnlyList<PlacementHistoryDto> Items, int Total)> GetHistoryAsync(
        Guid storeId,
        Guid skuId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ProductPlacementHistories
            .AsNoTracking()
            .Where(h => h.SkuId == skuId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(h => h.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new PlacementHistoryDto(
                h.Id,
                h.SkuId,
                h.Action,
                h.FromShelfLevelId,
                h.ToShelfLevelId,
                h.PerformedBy,
                h.Note,
                h.CreatedAt))
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<List<ProductPlacement>> GetPlacementsByShelfLevelIdAsync(
        Guid shelfLevelId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ProductPlacements
            .Where(p => p.ShelfLevelId == shelfLevelId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ProductPlacement>> GetPlacementsByLayoutIdAsync(
        Guid layoutId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ProductPlacements
            .Include(p => p.ShelfLevel)
                .ThenInclude(l => l!.Shelf)
            .Where(p => p.ShelfLevel != null && p.ShelfLevel.Shelf != null && p.ShelfLevel.Shelf.LayoutId == layoutId)
            .ToListAsync(cancellationToken);
    }

    public void Add(ProductPlacement placement) => dbContext.ProductPlacements.Add(placement);

    public void Remove(ProductPlacement placement) => dbContext.ProductPlacements.Remove(placement);

    public void AddHistory(ProductPlacementHistory history) => dbContext.ProductPlacementHistories.Add(history);
}
