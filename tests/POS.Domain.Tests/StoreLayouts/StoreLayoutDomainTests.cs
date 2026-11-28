using FluentAssertions;
using POS.Domain.Common;
using POS.Domain.StoreLayouts;
using POS.Domain.StoreLayouts.Enums;
using POS.Domain.StoreLayouts.Errors;
using Xunit;

namespace POS.Domain.Tests.StoreLayouts;

public class StoreLayoutDomainTests
{
    private readonly Guid _layoutId = Guid.NewGuid();
    private readonly Guid _storeId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _now = DateTime.UtcNow;

    private StoreLayout CreateTestLayout(int width = 10, int height = 10, string name = "Mặt bằng v1")
    {
        return StoreLayout.Create(
            storeId: _storeId,
            versionNumber: 1,
            name: name,
            gridWidth: width,
            gridHeight: height,
            createdBy: _userId,
            now: _now,
            id: _layoutId).Value;
    }

    [Fact]
    public void Create_should_initialize_draft_layout_with_walkway_cells_and_revision_1()
    {
        // Act
        var result = StoreLayout.Create(
            storeId: _storeId,
            versionNumber: 1,
            name: "Mặt bằng v1",
            gridWidth: 20,
            gridHeight: 15,
            createdBy: _userId,
            now: _now,
            id: _layoutId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var layout = result.Value;
        layout.StoreId.Should().Be(_storeId);
        layout.Name.Should().Be("Mặt bằng v1");
        layout.GridWidth.Should().Be(20);
        layout.GridHeight.Should().Be(15);
        layout.Status.Should().Be(StoreLayoutStatus.Draft);
        layout.Revision.Should().Be(1);
        layout.VersionNumber.Should().Be(1);
        layout.CreatedBy.Should().Be(_userId);
        layout.PublishedAt.Should().BeNull();
        layout.PublishedBy.Should().BeNull();
        layout.Cells.Should().HaveCount(20 * 15);
        layout.Cells.Should().AllSatisfy(c => c.CellType.Should().Be(LayoutCellType.Walkway));
    }

    [Fact]
    public void Publish_without_entrance_cell_should_fail_with_EntranceRequired()
    {
        // Arrange
        var layout = CreateTestLayout();

        // Act
        var result = layout.Publish(_userId, expectedRevision: 1, _now);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(StoreLayoutErrors.EntranceRequired.Code);
    }

    [Fact]
    public void Publish_with_entrance_cell_should_succeed_and_increment_revision()
    {
        // Arrange
        var layout = CreateTestLayout();
        layout.UpdateCells([(0, 0, LayoutCellType.Entrance, 1)], expectedRevision: 1, _now);

        // Act
        var result = layout.Publish(_userId, expectedRevision: 2, _now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        layout.Status.Should().Be(StoreLayoutStatus.Active);
        layout.PublishedBy.Should().Be(_userId);
        layout.PublishedAt.Should().Be(_now);
        layout.Revision.Should().Be(3);
    }

    [Fact]
    public void AddShelf_on_Active_layout_should_fail_with_NotDraft()
    {
        // Arrange
        var layout = CreateTestLayout();
        layout.UpdateCells([(0, 0, LayoutCellType.Entrance, 1)], expectedRevision: 1, _now);
        layout.Publish(_userId, expectedRevision: 2, _now);

        var shelf = new Shelf(
            layoutId: layout.Id,
            zoneId: null,
            code: "S01",
            name: "Kệ 1",
            positionX: 2,
            positionY: 2,
            width: 3,
            height: 2,
            orientation: ShelfOrientation.North,
            now: _now);

        // Act
        var result = layout.AddShelf(shelf, expectedRevision: 3, _now);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(StoreLayoutErrors.NotDraft.Code);
    }

    [Fact]
    public void AddShelf_out_of_bounds_should_fail_with_ShelfOutOfBounds()
    {
        // Arrange
        var layout = CreateTestLayout();

        var shelf = new Shelf(
            layoutId: layout.Id,
            zoneId: null,
            code: "S01",
            name: "Kệ Ngoài Biên",
            positionX: 9,
            positionY: 9,
            width: 3,
            height: 2,
            orientation: ShelfOrientation.North,
            now: _now);

        // Act
        var result = layout.AddShelf(shelf, expectedRevision: 1, _now);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(StoreLayoutErrors.ShelfOutOfBounds.Code);
    }

    [Fact]
    public void AddShelf_overlapping_another_shelf_should_fail_with_ShelfOverlap()
    {
        // Arrange
        var layout = CreateTestLayout();

        var shelf1 = new Shelf(layout.Id, null, "S01", "Kệ 1", 2, 2, 3, 2, ShelfOrientation.North, _now);
        layout.AddShelf(shelf1, expectedRevision: 1, _now);

        var shelf2 = new Shelf(layout.Id, null, "S02", "Kệ 2", 3, 2, 3, 2, ShelfOrientation.North, _now);

        // Act
        var result = layout.AddShelf(shelf2, expectedRevision: 2, _now);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(StoreLayoutErrors.ShelfOverlap.Code);
    }

    [Fact]
    public void Archive_should_change_status_to_Archived_and_increment_revision()
    {
        // Arrange
        var layout = CreateTestLayout();

        // Act
        var result = layout.Archive(_userId, _now);

        // Assert
        result.IsSuccess.Should().BeTrue();
        layout.Status.Should().Be(StoreLayoutStatus.Archived);
        layout.Revision.Should().Be(2);
    }
}
