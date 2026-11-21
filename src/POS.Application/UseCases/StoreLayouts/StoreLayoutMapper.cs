using POS.Domain.StoreLayouts;

namespace POS.Application.UseCases.StoreLayouts;

public static class StoreLayoutMapper
{
    public static StoreLayoutSummaryDto ToSummaryDto(StoreLayout layout)
    {
        return new StoreLayoutSummaryDto(
            layout.Id,
            layout.StoreId,
            layout.VersionNumber,
            layout.Name,
            layout.GridWidth,
            layout.GridHeight,
            layout.Status,
            layout.Revision,
            layout.CreatedAt,
            layout.PublishedAt);
    }

    public static StoreLayoutDetailDto ToDetailDto(StoreLayout layout)
    {
        var zones = layout.Zones.Select(z => new StoreZoneDto(
            z.Id,
            z.LayoutId,
            z.Code,
            z.Name,
            z.Color,
            z.PositionX,
            z.PositionY,
            z.Width,
            z.Height)).ToList();

        var cells = layout.Cells.Select(c => new StoreLayoutCellDto(
            c.Id,
            c.X,
            c.Y,
            c.CellType,
            c.TraversalCost)).ToList();

        var shelves = layout.Shelves.Select(s => new ShelfDto(
            s.Id,
            s.LayoutId,
            s.ZoneId,
            s.Code,
            s.Name,
            s.PositionX,
            s.PositionY,
            s.Width,
            s.Height,
            s.Orientation,
            s.Status,
            s.Levels.Select(l => new ShelfLevelDto(
                l.Id,
                l.ShelfId,
                l.LevelNumber,
                l.Label,
                l.SortOrder,
                l.Placements.Select(p => new ProductPlacementDto(
                    p.Id,
                    p.ShelfLevelId,
                    p.SkuId,
                    p.IsPrimary,
                    p.Note,
                    p.UpdatedBy,
                    p.UpdatedAt,
                    layout.Revision)).ToList())).ToList())).ToList();

        return new StoreLayoutDetailDto(
            layout.Id,
            layout.StoreId,
            layout.VersionNumber,
            layout.Name,
            layout.GridWidth,
            layout.GridHeight,
            layout.Status,
            layout.Revision,
            layout.CreatedBy,
            layout.CreatedAt,
            layout.PublishedBy,
            layout.PublishedAt,
            zones,
            cells,
            shelves);
    }
}
