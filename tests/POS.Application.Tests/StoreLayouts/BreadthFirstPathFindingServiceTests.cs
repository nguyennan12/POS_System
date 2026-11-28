using FluentAssertions;
using POS.Application.UseCases.StoreLayouts;
using POS.Application.UseCases.StoreLayouts.Services;
using POS.Domain.StoreLayouts.Enums;
using Xunit;

namespace POS.Application.Tests.StoreLayouts;

public class BreadthFirstPathFindingServiceTests
{
    private readonly BreadthFirstPathFindingService _service = new();

    [Fact]
    public void FindShortestPath_on_clear_grid_should_find_direct_path()
    {
        // Arrange
        int width = 5;
        int height = 5;
        var unwalkable = new HashSet<(int X, int Y)>();
        var start = new LayoutPointDto(0, 0);
        var target = new LayoutPointDto(0, 3);

        // Act
        var path = _service.FindShortestPath(width, height, unwalkable, start, target);

        // Assert
        path.Should().NotBeNull();
        path.Should().HaveCount(4); // (0,0), (0,1), (0,2), (0,3)
        path![0].Should().Be(start);
        path[^1].Should().Be(target);
    }

    [Fact]
    public void FindShortestPath_should_navigate_around_obstacles()
    {
        // Arrange
        int width = 5;
        int height = 5;
        // Obstacle wall at (0, 1) and (1, 1)
        var unwalkable = new HashSet<(int X, int Y)> { (0, 1), (1, 1) };
        var start = new LayoutPointDto(0, 0);
        var target = new LayoutPointDto(0, 2);

        // Act
        var path = _service.FindShortestPath(width, height, unwalkable, start, target);

        // Assert
        path.Should().NotBeNull();
        path.Should().NotContain(new LayoutPointDto(0, 1));
        path.Should().NotContain(new LayoutPointDto(1, 1));
        path![0].Should().Be(start);
        path[^1].Should().Be(target);
        path.Count.Should().BeGreaterThan(3);
    }

    [Fact]
    public void FindShortestPath_when_target_is_trapped_should_return_null()
    {
        // Arrange
        int width = 5;
        int height = 5;
        // Surround target (4, 4) with unwalkable cells at (3, 4) and (4, 3)
        var unwalkable = new HashSet<(int X, int Y)> { (3, 4), (4, 3) };
        var start = new LayoutPointDto(0, 0);
        var target = new LayoutPointDto(4, 4);

        // Act
        var path = _service.FindShortestPath(width, height, unwalkable, start, target);

        // Assert
        path.Should().BeNull();
    }

    [Fact]
    public void FindShortestPath_should_be_deterministic()
    {
        // Arrange
        int width = 8;
        int height = 8;
        var unwalkable = new HashSet<(int X, int Y)> { (2, 2), (2, 3), (2, 4) };
        var start = new LayoutPointDto(1, 1);
        var target = new LayoutPointDto(6, 6);

        // Act
        var run1 = _service.FindShortestPath(width, height, unwalkable, start, target);
        var run2 = _service.FindShortestPath(width, height, unwalkable, start, target);

        // Assert
        run1.Should().NotBeNull();
        run2.Should().NotBeNull();
        run1.Should().Equal(run2);
    }

    [Fact]
    public void CalculatePickingRoute_should_order_stops_by_nearest_neighbor_and_build_continuous_path()
    {
        // Arrange
        var layoutId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        // 10x10 layout with entrance at (0, 0)
        var cells = new List<StoreLayoutCellDto>();
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                var type = (x == 0 && y == 0) ? LayoutCellType.Entrance : LayoutCellType.Walkway;
                cells.Add(new StoreLayoutCellDto(Guid.NewGuid(), x, y, type, 1));
            }
        }

        var shelf1 = new ShelfDto(Guid.NewGuid(), layoutId, null, "K01", "Kệ 1", 2, 1, 1, 2, ShelfOrientation.North, ShelfStatus.Active, []);
        var shelf2 = new ShelfDto(Guid.NewGuid(), layoutId, null, "K02", "Kệ 2", 5, 1, 1, 2, ShelfOrientation.North, ShelfStatus.Active, []);
        var shelf3 = new ShelfDto(Guid.NewGuid(), layoutId, null, "K03", "Kệ 3", 8, 1, 1, 2, ShelfOrientation.North, ShelfStatus.Active, []);

        var layoutDto = new StoreLayoutDetailDto(
            Id: layoutId,
            StoreId: storeId,
            VersionNumber: 1,
            Name: "Test Layout",
            GridWidth: 10,
            GridHeight: 10,
            Status: StoreLayoutStatus.Active,
            Revision: 2,
            CreatedBy: Guid.NewGuid(),
            CreatedAt: DateTime.UtcNow,
            PublishedBy: Guid.NewGuid(),
            PublishedAt: DateTime.UtcNow,
            Zones: [],
            Cells: cells,
            Shelves: [shelf1, shelf2, shelf3]);

        var skuA = Guid.NewGuid();
        var skuB = Guid.NewGuid();
        var skuC = Guid.NewGuid();

        var zoneId = Guid.NewGuid();
        var locA = new ProductLocationDto(zoneId, "Khu A", shelf1.Id, "K01", "Kệ 1", Guid.NewGuid(), 1, true, 2, 0);
        var locB = new ProductLocationDto(zoneId, "Khu A", shelf2.Id, "K02", "Kệ 2", Guid.NewGuid(), 2, true, 5, 0);
        var locC = new ProductLocationDto(zoneId, "Khu A", shelf3.Id, "K03", "Kệ 3", Guid.NewGuid(), 1, true, 8, 0);

        // Pass in reverse order: C, B, A
        var items = new List<(Guid SkuId, decimal Quantity, string SkuCode, string ProductName, IReadOnlyList<ProductLocationDto> Locations)>
        {
            (skuC, 1, "SKU-C", "Sản phẩm C", [locC]),
            (skuB, 1, "SKU-B", "Sản phẩm B", [locB]),
            (skuA, 1, "SKU-A", "Sản phẩm A", [locA])
        };

        // Act
        var preview = _service.CalculatePickingRoute(layoutDto, requestedStart: new LayoutPointDto(0, 0), items);

        // Assert
        preview.OrderedStops.Should().HaveCount(3);
        // Ordered stops should start with nearest from (0,0): K01 (skuA), then K02 (skuB), then K03 (skuC)
        preview.OrderedStops[0].SkuId.Should().Be(skuA);
        preview.OrderedStops[0].StopIndex.Should().Be(1);
        preview.OrderedStops[1].SkuId.Should().Be(skuB);
        preview.OrderedStops[1].StopIndex.Should().Be(2);
        preview.OrderedStops[2].SkuId.Should().Be(skuC);
        preview.OrderedStops[2].StopIndex.Should().Be(3);

        preview.PathCells.Should().NotBeEmpty();
        preview.MissingSkuIds.Should().BeEmpty();
        preview.UnreachableSkuIds.Should().BeEmpty();
        preview.RouteNotice.Should().NotBeNullOrWhiteSpace();
    }
}
