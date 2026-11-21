using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using POS.Contracts.V1.StoreLayouts;

namespace POS.WinUI.ViewModels.SmartStore.Models;

public static class CanvasGeometryConstants
{
    public const double DefaultCellSize = 32.0;
}

public partial class CanvasCellVisual : ObservableObject
{
    public int X { get; set; }
    public int Y { get; set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackgroundBrush))]
    [NotifyPropertyChangedFor(nameof(MarkerLabel))]
    private string _cellType = "Walkway";
    [ObservableProperty] private double _canvasLeft;
    [ObservableProperty] private double _canvasTop;
    [ObservableProperty] private double _size = CanvasGeometryConstants.DefaultCellSize;

    public Brush BackgroundBrush => CellType switch
    {
        "Blocked" => new SolidColorBrush(Color.FromRgb(203, 213, 225)), // Slate 300
        "Entrance" => new SolidColorBrush(Color.FromRgb(34, 197, 94)),  // Green 500
        "Checkout" => new SolidColorBrush(Color.FromRgb(237, 122, 28)), // Brand Orange
        _ => Brushes.Transparent                                      // Walkway
    };

    public string MarkerLabel => CellType switch
    {
        "Entrance" => "LỐI VÀO",
        "Checkout" => "THU NGÂN",
        _ => string.Empty
    };
}

public partial class CanvasZoneVisual : ObservableObject
{
    public Guid Id { get; set; }
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _colorHex = "#3B82F6";
    [ObservableProperty] private double _canvasLeft;
    [ObservableProperty] private double _canvasTop;
    [ObservableProperty] private double _canvasWidth;
    [ObservableProperty] private double _canvasHeight;
    [ObservableProperty] private bool _isSelected;

    public int GridX { get; set; }
    public int GridY { get; set; }
    public int GridWidth { get; set; }
    public int GridHeight { get; set; }
}

public partial class CanvasShelfVisual : ObservableObject
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Orientation { get; set; } = "North";
    public int TotalPlacements { get; set; }

    [ObservableProperty] private double _canvasLeft;
    [ObservableProperty] private double _canvasTop;
    [ObservableProperty] private double _canvasWidth;
    [ObservableProperty] private double _canvasHeight;
    [ObservableProperty] private bool _isSelected;

    public int GridX { get; set; }
    public int GridY { get; set; }
    public int GridWidth { get; set; }
    public int GridHeight { get; set; }
}

public class CanvasRouteStopVisual
{
    public int StopIndex { get; set; }
    public string SkuCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string ShelfCode { get; set; } = string.Empty;
    public int LevelNumber { get; set; }
    public double CanvasX { get; set; }
    public double CanvasY { get; set; }
}
