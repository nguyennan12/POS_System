using POS.Contracts.V1.StoreLayouts;
using POS.WinUI.ViewModels.SmartStore.Models;

namespace POS.WinUI.ViewModels.SmartStore;

public sealed partial class StoreLayoutViewModel
{
    private void WireSubViewModelEvents()
    {
        MapVm.ShelfSelected += OnShelfSelectedOnMap;
        EditorVm.LayoutMutated += OnLayoutMutated;
        EditorVm.RequestCloneCurrentLayout += OpenCloneDialog;
        EditorVm.RequestReloadLayout += OnRequestReloadLayout;
        EditorVm.CellsReverted += OnCellsReverted;
        PlacementVm.PlacementMutated += OnPlacementMutated;
        PickingVm.RouteCalculated += OnRouteCalculated;
        PickingVm.RouteCleared += OnRouteCleared;
    }

    private void UnwireSubViewModelEvents()
    {
        MapVm.ShelfSelected -= OnShelfSelectedOnMap;
        EditorVm.LayoutMutated -= OnLayoutMutated;
        EditorVm.RequestCloneCurrentLayout -= OpenCloneDialog;
        EditorVm.RequestReloadLayout -= OnRequestReloadLayout;
        EditorVm.CellsReverted -= OnCellsReverted;
        PlacementVm.PlacementMutated -= OnPlacementMutated;
        PickingVm.RouteCalculated -= OnRouteCalculated;
        PickingVm.RouteCleared -= OnRouteCleared;
    }

    private void OnCellsReverted(IReadOnlyList<(int X, int Y, string OldType)> cells)
    {
        foreach (var (x, y, oldType) in cells)
        {
            var visual = MapVm.Cells.FirstOrDefault(c => c.X == x && c.Y == y);
            if (visual != null)
            {
                visual.CellType = oldType;
            }
        }
    }

    private void OnShelfSelectedOnMap(CanvasShelfVisual? visual)
    {
        if (visual == null || CurrentLayout == null) return;

        var shelf = CurrentLayout.Shelves.FirstOrDefault(s => s.Id == visual.Id);
        if (shelf != null)
        {
            PlacementVm.PreselectShelf(shelf);
            if (ActiveMode != "Placement" && ActiveMode != "Design")
            {
                // Tự động chuyển tab Gán Vị Trí SKU để nhân viên xem hoặc gán sản phẩm vào kệ này
                ActiveMode = "Placement";
            }
        }
    }

    private async void OnLayoutMutated()
    {
        await ReloadCurrentLayoutQuietAsync();
    }

    private async void OnRequestReloadLayout()
    {
        await ReloadCurrentLayoutAsync();
    }

    private async void OnPlacementMutated(int newRevision)
    {
        if (CurrentLayout != null)
        {
            CurrentLayout.Revision = newRevision;
            await ReloadCurrentLayoutQuietAsync();
        }
    }

    private void OnRouteCalculated(PickingRoutePreviewResponse response)
    {
        MapVm.SetRoutePreview(response);
        ShowInfo($"Đã dựng lộ trình picking: {response.OrderedStops.Count} điểm dừng, {response.PathCells.Count} bước di chuyển.");
    }

    private void OnRouteCleared()
    {
        MapVm.ClearRoute();
    }
}
