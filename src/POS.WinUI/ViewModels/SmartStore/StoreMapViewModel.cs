using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.StoreLayouts;
using POS.WinUI.ViewModels.SmartStore.Models;

namespace POS.WinUI.ViewModels.SmartStore;

public partial class StoreMapViewModel : ObservableObject
{
    public const double CellSize = CanvasGeometryConstants.DefaultCellSize;

    [ObservableProperty] private double _zoom = 1.0;
    [ObservableProperty] private double _panX = 0;
    [ObservableProperty] private double _panY = 0;
    [ObservableProperty] private double _canvasWidth = 1000;
    [ObservableProperty] private double _canvasHeight = 800;
    [ObservableProperty] private bool _hasLayout = false;
    [ObservableProperty] private CanvasShelfVisual? _selectedShelfVisual;

    public ObservableCollection<CanvasCellVisual> Cells { get; } = [];
    public ObservableCollection<CanvasZoneVisual> Zones { get; } = [];
    public ObservableCollection<CanvasShelfVisual> Shelves { get; } = [];
    public ObservableCollection<CanvasRouteStopVisual> RouteStops { get; } = [];

    [ObservableProperty] private PointCollection _routePoints = [];

    public event Action<CanvasShelfVisual?>? ShelfSelected;

    public void LoadLayoutGeometry(StoreLayoutDetailModel layout)
    {
        Cells.Clear();
        Zones.Clear();
        Shelves.Clear();
        RouteStops.Clear();
        RoutePoints = [];
        HasLayout = true;

        CanvasWidth = layout.GridWidth * CellSize;
        CanvasHeight = layout.GridHeight * CellSize;

        // 1. Cells
        foreach (var c in layout.Cells)
        {
            Cells.Add(new CanvasCellVisual
            {
                X = c.X,
                Y = c.Y,
                CellType = c.CellType,
                CanvasLeft = c.X * CellSize,
                CanvasTop = c.Y * CellSize,
                Size = CellSize
            });
        }

        // 2. Zones
        foreach (var z in layout.Zones)
        {
            Zones.Add(new CanvasZoneVisual
            {
                Id = z.Id,
                Code = z.Code,
                Name = z.Name,
                ColorHex = z.Color,
                GridX = z.PositionX,
                GridY = z.PositionY,
                GridWidth = z.Width,
                GridHeight = z.Height,
                CanvasLeft = z.PositionX * CellSize,
                CanvasTop = z.PositionY * CellSize,
                CanvasWidth = z.Width * CellSize,
                CanvasHeight = z.Height * CellSize,
                IsSelected = false
            });
        }

        // 3. Shelves
        foreach (var s in layout.Shelves)
        {
            Shelves.Add(new CanvasShelfVisual
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Orientation = s.Orientation,
                TotalPlacements = s.TotalPlacementsCount,
                GridX = s.PositionX,
                GridY = s.PositionY,
                GridWidth = s.Width,
                GridHeight = s.Height,
                CanvasLeft = s.PositionX * CellSize,
                CanvasTop = s.PositionY * CellSize,
                CanvasWidth = s.Width * CellSize,
                CanvasHeight = s.Height * CellSize,
                IsSelected = false
            });
        }
    }

    public void SetRoutePreview(PickingRoutePreviewResponse route)
    {
        RouteStops.Clear();
        var points = new PointCollection();

        foreach (var cell in route.PathCells)
        {
            double cx = cell.X * CellSize + (CellSize / 2.0);
            double cy = cell.Y * CellSize + (CellSize / 2.0);
            points.Add(new Point(cx, cy));
        }

        RoutePoints = points;

        foreach (var stop in route.OrderedStops)
        {
            double sx = stop.PickCellX * CellSize + (CellSize / 2.0);
            double sy = stop.PickCellY * CellSize + (CellSize / 2.0);
            RouteStops.Add(new CanvasRouteStopVisual
            {
                StopIndex = stop.StopIndex,
                SkuCode = stop.SkuCode,
                ProductName = stop.ProductName,
                Quantity = stop.Quantity,
                ShelfCode = stop.ShelfCode,
                LevelNumber = stop.LevelNumber,
                CanvasX = sx,
                CanvasY = sy
            });
        }
    }

    public void ClearRoute()
    {
        RouteStops.Clear();
        RoutePoints = [];
    }

    public void ClearPickingRoute() => ClearRoute();

    [RelayCommand]
    public void SelectShelf(CanvasShelfVisual? shelf)
    {
        foreach (var s in Shelves)
        {
            s.IsSelected = (shelf != null && s.Id == shelf.Id);
        }
        SelectedShelfVisual = shelf;
        ShelfSelected?.Invoke(shelf);
    }

    [RelayCommand]
    public void ZoomIn()
    {
        if (Zoom < 3.0) Zoom = Math.Min(3.0, Zoom + 0.15);
    }

    [RelayCommand]
    public void ZoomOut()
    {
        if (Zoom > 0.3) Zoom = Math.Max(0.3, Zoom - 0.15);
    }

    [RelayCommand]
    public void ResetZoom()
    {
        Zoom = 1.0;
        PanX = 0;
        PanY = 0;
    }

    [RelayCommand]
    public void FitToView(Size viewportSize)
    {
        if (CanvasWidth <= 0 || CanvasHeight <= 0 || viewportSize.Width <= 0 || viewportSize.Height <= 0)
            return;

        double scaleX = viewportSize.Width / CanvasWidth;
        double scaleY = viewportSize.Height / CanvasHeight;
        Zoom = Math.Max(0.3, Math.Min(2.0, Math.Min(scaleX, scaleY) * 0.9));
        PanX = 0;
        PanY = 0;
    }
}
