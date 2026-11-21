using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using POS.Contracts.V1.StoreLayouts;
using POS.Domain.StoreLayouts.Enums;

namespace POS.WinUI.ViewModels.SmartStore.Models;

public partial class StoreLayoutSummaryItem : ObservableObject
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public int VersionNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public int GridWidth { get; set; }
    public int GridHeight { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Revision { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }

    public bool IsActive => Status.Equals("Active", StringComparison.OrdinalIgnoreCase);
    public bool IsDraft => Status.Equals("Draft", StringComparison.OrdinalIgnoreCase);
    public bool IsArchived => Status.Equals("Archived", StringComparison.OrdinalIgnoreCase);

    public string StatusBadgeText => Status switch
    {
        "Active" => "Đang hoạt động",
        "Draft" => "Bản thảo",
        "Archived" => "Đã lưu trữ",
        _ => Status
    };
}

public partial class StoreLayoutDetailModel : ObservableObject
{
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private Guid _storeId;
    [ObservableProperty] private int _versionNumber;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private int _gridWidth;
    [ObservableProperty] private int _gridHeight;
    [ObservableProperty] private string _status = string.Empty;
    [ObservableProperty] private int _revision;
    [ObservableProperty] private Guid _createdBy;
    [ObservableProperty] private DateTime _createdAt;
    [ObservableProperty] private Guid? _publishedBy;
    [ObservableProperty] private DateTime? _publishedAt;

    public ObservableCollection<StoreZoneModel> Zones { get; } = [];
    public ObservableCollection<StoreLayoutCellModel> Cells { get; } = [];
    public ObservableCollection<ShelfModel> Shelves { get; } = [];

    public bool IsActive => Status.Equals("Active", StringComparison.OrdinalIgnoreCase);
    public bool IsDraft => Status.Equals("Draft", StringComparison.OrdinalIgnoreCase);
    public bool IsArchived => Status.Equals("Archived", StringComparison.OrdinalIgnoreCase);
}

public partial class StoreZoneModel : ObservableObject
{
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private Guid _layoutId;
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _color = "#3B82F6";
    [ObservableProperty] private int _positionX;
    [ObservableProperty] private int _positionY;
    [ObservableProperty] private int _width;
    [ObservableProperty] private int _height;
}

public partial class StoreLayoutCellModel : ObservableObject
{
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private int _x;
    [ObservableProperty] private int _y;
    [ObservableProperty] private string _cellType = "Walkway";
    [ObservableProperty] private int _traversalCost = 1;

    public bool IsWalkway => CellType.Equals("Walkway", StringComparison.OrdinalIgnoreCase);
    public bool IsBlocked => CellType.Equals("Blocked", StringComparison.OrdinalIgnoreCase);
    public bool IsEntrance => CellType.Equals("Entrance", StringComparison.OrdinalIgnoreCase);
    public bool IsCheckout => CellType.Equals("Checkout", StringComparison.OrdinalIgnoreCase);
}

public partial class ShelfModel : ObservableObject
{
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private Guid _layoutId;
    [ObservableProperty] private Guid? _zoneId;
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private int _positionX;
    [ObservableProperty] private int _positionY;
    [ObservableProperty] private int _width;
    [ObservableProperty] private int _height;
    [ObservableProperty] private string _orientation = "North";
    [ObservableProperty] private string _status = "Active";

    public ObservableCollection<ShelfLevelModel> Levels { get; } = [];

    public int TotalPlacementsCount => Levels.Sum(l => l.Placements.Count);
}

public partial class ShelfLevelModel : ObservableObject
{
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private Guid _shelfId;
    [ObservableProperty] private int _levelNumber;
    [ObservableProperty] private string? _label;
    [ObservableProperty] private int _sortOrder;

    public ObservableCollection<ProductPlacementModel> Placements { get; } = [];

    public string DisplayTitle => string.IsNullOrWhiteSpace(Label) ? $"Tầng {LevelNumber}" : $"Tầng {LevelNumber} - {Label}";
}

public partial class ProductPlacementModel : ObservableObject
{
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private Guid _shelfLevelId;
    [ObservableProperty] private Guid _skuId;
    [ObservableProperty] private bool _isPrimary;
    [ObservableProperty] private string? _note;
    [ObservableProperty] private Guid _updatedBy;
    [ObservableProperty] private DateTime _updatedAt;
    [ObservableProperty] private int _layoutRevision;
}
