using POS.Application.Abstractions.StoreLayouts;
using POS.Domain.StoreLayouts.Enums;

namespace POS.Application.UseCases.StoreLayouts.Services;

public class BreadthFirstPathFindingService : IPathFindingService
{
    private static readonly (int DX, int DY)[] Directions =
    [
        (0, -1), // North
        (1, 0),  // East
        (0, 1),  // South
        (-1, 0)  // West
    ];

    public List<LayoutPointDto>? FindShortestPath(
        int gridWidth,
        int gridHeight,
        HashSet<(int X, int Y)> unwalkable,
        LayoutPointDto start,
        LayoutPointDto target)
    {
        if (start.X == target.X && start.Y == target.Y)
            return [start];

        if (unwalkable.Contains((start.X, start.Y)) || unwalkable.Contains((target.X, target.Y)))
            return null;

        var visited = new HashSet<(int X, int Y)> { (start.X, start.Y) };
        var parentMap = new Dictionary<(int X, int Y), (int X, int Y)>();
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((start.X, start.Y));

        bool found = false;

        while (queue.Count > 0)
        {
            var curr = queue.Dequeue();
            if (curr.X == target.X && curr.Y == target.Y)
            {
                found = true;
                break;
            }

            foreach (var (dx, dy) in Directions)
            {
                int nx = curr.X + dx;
                int ny = curr.Y + dy;

                if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight)
                    continue;

                var nextKey = (nx, ny);
                if (unwalkable.Contains(nextKey) || visited.Contains(nextKey))
                    continue;

                visited.Add(nextKey);
                parentMap[nextKey] = curr;
                queue.Enqueue(nextKey);
            }
        }

        if (!found) return null;

        var path = new List<LayoutPointDto>();
        var step = (target.X, target.Y);
        while (step != (start.X, start.Y))
        {
            path.Add(new LayoutPointDto(step.Item1, step.Item2));
            step = parentMap[step];
        }
        path.Add(start);
        path.Reverse();
        return path;
    }

    public PickingRoutePreviewDto CalculatePickingRoute(
        StoreLayoutDetailDto layout,
        LayoutPointDto? requestedStart,
        IReadOnlyList<(Guid SkuId, decimal Quantity, string SkuCode, string ProductName, IReadOnlyList<ProductLocationDto> Locations)> items)
    {
        // 1. Build unwalkable set (Blocked cells + Shelf footprints)
        var unwalkable = new HashSet<(int X, int Y)>();

        foreach (var cell in layout.Cells)
        {
            if (cell.CellType == LayoutCellType.Blocked)
            {
                unwalkable.Add((cell.X, cell.Y));
            }
        }

        foreach (var shelf in layout.Shelves)
        {
            for (int sy = shelf.PositionY; sy < shelf.PositionY + shelf.Height; sy++)
            {
                for (int sx = shelf.PositionX; sx < shelf.PositionX + shelf.Width; sx++)
                {
                    unwalkable.Add((sx, sy));
                }
            }
        }

        // 2. Resolve start cell: requestedStart or first Entrance
        LayoutPointDto startPoint;
        if (requestedStart != null && !unwalkable.Contains((requestedStart.X, requestedStart.Y)))
        {
            startPoint = requestedStart;
        }
        else
        {
            var entrance = layout.Cells.FirstOrDefault(c => c.CellType == LayoutCellType.Entrance)
                           ?? layout.Cells.FirstOrDefault(c => c.CellType == LayoutCellType.Walkway);
            startPoint = entrance != null ? new LayoutPointDto(entrance.X, entrance.Y) : new LayoutPointDto(0, 0);
        }

        var missingSkuIds = new List<Guid>();
        var unreachableSkuIds = new List<Guid>();
        var candidateItems = new List<PickCandidate>();

        // 3. For each requested SKU, resolve candidate reachable pickup locations
        foreach (var item in items)
        {
            if (item.Locations == null || item.Locations.Count == 0)
            {
                missingSkuIds.Add(item.SkuId);
                continue;
            }

            var reachableLocations = new List<(ProductLocationDto Loc, List<LayoutPointDto> PathFromStart)>();

            foreach (var loc in item.Locations)
            {
                var pickPoint = new LayoutPointDto(loc.PickCellX, loc.PickCellY);
                if (unwalkable.Contains((pickPoint.X, pickPoint.Y)))
                {
                    // Fallback: inspect any adjacent cell of the shelf
                    var shelf = layout.Shelves.FirstOrDefault(s => s.Id == loc.ShelfId);
                    if (shelf != null)
                    {
                        var adj = GetWalkableAdjacentCells(layout.GridWidth, layout.GridHeight, shelf.PositionX, shelf.PositionY, shelf.Width, shelf.Height, unwalkable);
                        if (adj.Count > 0)
                        {
                            pickPoint = adj[0];
                        }
                    }
                }

                var path = FindShortestPath(layout.GridWidth, layout.GridHeight, unwalkable, startPoint, pickPoint);
                if (path != null)
                {
                    reachableLocations.Add((loc with { PickCellX = pickPoint.X, PickCellY = pickPoint.Y }, path));
                }
            }

            if (reachableLocations.Count == 0)
            {
                unreachableSkuIds.Add(item.SkuId);
                continue;
            }

            // Pick the reachable location that is primary or closest
            var best = reachableLocations
                .OrderByDescending(r => r.Loc.IsPrimary)
                .ThenBy(r => r.PathFromStart.Count)
                .First();

            candidateItems.Add(new PickCandidate(
                item.SkuId,
                item.Quantity,
                item.SkuCode,
                item.ProductName,
                best.Loc));
        }

        // 4. Sequence stops using Nearest-Neighbor heuristic
        var orderedStops = new List<PickingRouteStopDto>();
        var allPathCells = new List<LayoutPointDto>();
        var currentPoint = startPoint;
        var remainingCandidates = new List<PickCandidate>(candidateItems);

        int stopIndex = 1;
        while (remainingCandidates.Count > 0)
        {
            PickCandidate? nearestCandidate = null;
            List<LayoutPointDto>? bestSegment = null;
            int minDistance = int.MaxValue;

            foreach (var cand in remainingCandidates)
            {
                var targetPoint = new LayoutPointDto(cand.Location.PickCellX, cand.Location.PickCellY);
                var seg = FindShortestPath(layout.GridWidth, layout.GridHeight, unwalkable, currentPoint, targetPoint);
                if (seg != null && seg.Count < minDistance)
                {
                    minDistance = seg.Count;
                    nearestCandidate = cand;
                    bestSegment = seg;
                }
            }

            if (nearestCandidate == null || bestSegment == null)
            {
                // Remaining candidates couldn't be routed from currentPoint
                foreach (var remaining in remainingCandidates)
                {
                    unreachableSkuIds.Add(remaining.SkuId);
                }
                break;
            }

            // Append segment cells (avoid duplicate first cell)
            if (allPathCells.Count == 0)
            {
                allPathCells.AddRange(bestSegment);
            }
            else
            {
                for (int i = 1; i < bestSegment.Count; i++)
                {
                    allPathCells.Add(bestSegment[i]);
                }
            }

            orderedStops.Add(new PickingRouteStopDto(
                stopIndex++,
                nearestCandidate.SkuId,
                nearestCandidate.SkuCode,
                nearestCandidate.ProductName,
                nearestCandidate.Quantity,
                nearestCandidate.Location.ShelfId,
                nearestCandidate.Location.ShelfCode,
                nearestCandidate.Location.LevelNumber,
                nearestCandidate.Location.PickCellX,
                nearestCandidate.Location.PickCellY));

            currentPoint = new LayoutPointDto(nearestCandidate.Location.PickCellX, nearestCandidate.Location.PickCellY);
            remainingCandidates.Remove(nearestCandidate);
        }

        return new PickingRoutePreviewDto(
            layout.Id,
            layout.VersionNumber,
            startPoint,
            orderedStops,
            allPathCells,
            missingSkuIds,
            unreachableSkuIds,
            "Tuyến đường này là gợi ý tối ưu theo khoảng cách lân cận (Heuristic), không phải là nghiệm duy nhất.");
    }

    private static List<LayoutPointDto> GetWalkableAdjacentCells(
        int gridWidth,
        int gridHeight,
        int shelfX,
        int shelfY,
        int width,
        int height,
        HashSet<(int X, int Y)> unwalkable)
    {
        var result = new List<LayoutPointDto>();

        // Top edge (y - 1)
        int topY = shelfY - 1;
        if (topY >= 0)
        {
            for (int x = shelfX; x < shelfX + width; x++)
            {
                if (x >= 0 && x < gridWidth && !unwalkable.Contains((x, topY)))
                    result.Add(new LayoutPointDto(x, topY));
            }
        }

        // Bottom edge (y + height)
        int bottomY = shelfY + height;
        if (bottomY < gridHeight)
        {
            for (int x = shelfX; x < shelfX + width; x++)
            {
                if (x >= 0 && x < gridWidth && !unwalkable.Contains((x, bottomY)))
                    result.Add(new LayoutPointDto(x, bottomY));
            }
        }

        // Left edge (x - 1)
        int leftX = shelfX - 1;
        if (leftX >= 0)
        {
            for (int y = shelfY; y < shelfY + height; y++)
            {
                if (y >= 0 && y < gridHeight && !unwalkable.Contains((leftX, y)))
                    result.Add(new LayoutPointDto(leftX, y));
            }
        }

        // Right edge (x + width)
        int rightX = shelfX + width;
        if (rightX < gridWidth)
        {
            for (int y = shelfY; y < shelfY + height; y++)
            {
                if (y >= 0 && y < gridHeight && !unwalkable.Contains((rightX, y)))
                    result.Add(new LayoutPointDto(rightX, y));
            }
        }

        return result;
    }

    private record PickCandidate(
        Guid SkuId,
        decimal Quantity,
        string SkuCode,
        string ProductName,
        ProductLocationDto Location);
}
