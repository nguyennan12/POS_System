using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.StoreLayouts;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.ViewModels.SmartStore.Models;

namespace POS.WinUI.ViewModels.SmartStore;

public partial class LayoutEditorViewModel : ObservableObject
{
    private readonly StoreLayoutApiClient _layoutClient;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    private StoreLayoutDetailModel? _layout;
    [ObservableProperty] private string _selectedBrush = "Blocked"; // Select, Walkway, Blocked, Entrance, Checkout
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private string? _errorMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedZone))]
    private StoreZoneModel? _selectedZone;

    public bool HasSelectedZone => SelectedZone != null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedShelf))]
    private ShelfModel? _selectedShelf;

    public bool HasSelectedShelf => SelectedShelf != null;

    public bool CanEdit => Layout?.IsDraft == true;

    // Initial cell types when layout loaded: (X, Y) -> initial CellType
    private readonly Dictionary<(int X, int Y), string> _initialCellTypes = [];

    // Pending cell changes dictionary: (X, Y) -> CellType
    private readonly Dictionary<(int X, int Y), string> _pendingCellChanges = [];

    // Undo stack: each entry = list of (X, Y, oldCellType) before a paint stroke
    private readonly Stack<List<(int X, int Y, string OldType)>> _undoStack = new();
    private List<(int X, int Y, string OldType)>? _currentStroke;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUndo))]
    private int _undoCount; // triggers CanUndo binding refresh

    public bool CanUndo => _undoStack.Count > 0;

    public event Action? LayoutMutated;
    public event Action? RequestCloneCurrentLayout;
    public event Action? RequestReloadLayout;
    public event Action<IReadOnlyList<(int X, int Y, string OldType)>>? CellsReverted;

    public LayoutEditorViewModel(StoreLayoutApiClient layoutClient)
    {
        _layoutClient = layoutClient;
    }

    public void Initialize(StoreLayoutDetailModel layout)
    {
        Layout = layout;
        _initialCellTypes.Clear();
        foreach (var c in layout.Cells)
        {
            _initialCellTypes[(c.X, c.Y)] = c.CellType;
        }

        _pendingCellChanges.Clear();
        _undoStack.Clear();
        _currentStroke = null;
        UndoCount = 0;
        IsDirty = false;
        ErrorMessage = null;
        SelectedZone = null;
        SelectedShelf = null;
        if (layout.IsDraft)
        {
            StatusMessage = $"Chế độ chỉnh sửa bản thảo: {layout.Name} (Revision: #{layout.Revision})";
            if (SelectedBrush == "Select") SelectedBrush = "Blocked";
        }
        else
        {
            StatusMessage = $"Mặt bằng chính thức '{layout.Name}' đang hoạt động (Chỉ đọc). Bấm 'Nhân bản' để tạo bản thảo và chỉnh sửa.";
        }
        OnPropertyChanged(nameof(CanEdit));
    }

    [RelayCommand]
    public void CloneCurrentLayout()
    {
        RequestCloneCurrentLayout?.Invoke();
    }

    [RelayCommand]
    public void ReloadLayout()
    {
        RequestReloadLayout?.Invoke();
    }

    [RelayCommand]
    public void DiscardChanges()
    {
        if (Layout != null)
        {
            var revertedList = new List<(int X, int Y, string OldType)>();
            foreach (var kv in _pendingCellChanges)
            {
                var (x, y) = kv.Key;
                if (_initialCellTypes.TryGetValue((x, y), out var initType))
                {
                    var cell = Layout.Cells.FirstOrDefault(c => c.X == x && c.Y == y);
                    if (cell != null) cell.CellType = initType;
                    revertedList.Add((x, y, initType));
                }
            }
            if (revertedList.Count > 0)
            {
                CellsReverted?.Invoke(revertedList);
            }
        }

        _pendingCellChanges.Clear();
        _undoStack.Clear();
        _currentStroke = null;
        UndoCount = 0;
        IsDirty = false;
        ErrorMessage = null;
        StatusMessage = "Đã thu hồi và hủy các thay đổi ô lưới chưa lưu.";
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    public void UndoLastStroke()
    {
        if (_undoStack.Count == 0 || Layout == null) return;

        var stroke = _undoStack.Pop();
        UndoCount = _undoStack.Count;

        foreach (var (x, y, oldType) in stroke)
        {
            var cell = Layout.Cells.FirstOrDefault(c => c.X == x && c.Y == y);
            if (cell != null) cell.CellType = oldType;

            if (_initialCellTypes.TryGetValue((x, y), out var initType) && initType == oldType)
            {
                _pendingCellChanges.Remove((x, y));
            }
            else
            {
                _pendingCellChanges[(x, y)] = oldType;
            }
        }

        IsDirty = _pendingCellChanges.Count > 0;
        StatusMessage = $"Đã hoàn tác 1 nét vẽ ({stroke.Count} ô). Còn {_undoStack.Count} bước có thể hoàn tác.";
        CellsReverted?.Invoke(stroke);
    }

    /// <summary>Gọi khi bắt đầu 1 nét vẽ mới (MouseDown).</summary>
    public void BeginStroke()
    {
        _currentStroke = [];
    }

    /// <summary>Gọi khi kết thúc nét vẽ (MouseUp / MouseLeave).</summary>
    public void EndStroke()
    {
        if (_currentStroke is { Count: > 0 })
        {
            _undoStack.Push(_currentStroke);
            UndoCount = _undoStack.Count;
        }
        _currentStroke = null;
    }

    [RelayCommand]
    public void SelectZone(StoreZoneModel? zone)
    {
        SelectedZone = zone;
    }

    [RelayCommand]
    public void DeselectZone()
    {
        SelectedZone = null;
    }

    [RelayCommand]
    public async Task MoveSelectedZoneAsync(string direction)
    {
        if (Layout == null || SelectedZone == null) return;

        int newX = SelectedZone.PositionX;
        int newY = SelectedZone.PositionY;

        switch (direction)
        {
            case "Left":
                newX = Math.Max(0, newX - 1);
                break;
            case "Right":
                newX = Math.Min(Layout.GridWidth - SelectedZone.Width, newX + 1);
                break;
            case "Up":
                newY = Math.Max(0, newY - 1);
                break;
            case "Down":
                newY = Math.Min(Layout.GridHeight - SelectedZone.Height, newY + 1);
                break;
        }

        if (newX == SelectedZone.PositionX && newY == SelectedZone.PositionY) return;

        await UpdateZonePositionAsync(SelectedZone.Id, newX, newY);
    }

    public async Task UpdateZonePositionAsync(Guid zoneId, int newX, int newY)
    {
        if (Layout == null) return;
        var zone = Layout.Zones.FirstOrDefault(z => z.Id == zoneId);
        if (zone == null) return;

        IsSaving = true;
        ErrorMessage = null;
        try
        {
            var req = new UpdateZoneRequest(
                zone.Code,
                zone.Name,
                zone.Color,
                newX,
                newY,
                zone.Width,
                zone.Height,
                Layout.Revision);

            var res = await _layoutClient.UpdateZoneAsync(Layout.Id, zoneId, req);
            if (res?.Success == true && res.Data != null)
            {
                zone.PositionX = res.Data.PositionX;
                zone.PositionY = res.Data.PositionY;
                StatusMessage = $"Đã di chuyển khu vực {zone.Code} đến tọa độ ({zone.PositionX}, {zone.PositionY}).";
                LayoutMutated?.Invoke();
            }
            else
            {
                ErrorMessage = res?.Error?.Message ?? "Không thể di chuyển khu vực.";
                LayoutMutated?.Invoke();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            LayoutMutated?.Invoke();
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    public async Task DeleteSelectedZoneAsync()
    {
        if (Layout == null || SelectedZone == null) return;

        IsSaving = true;
        ErrorMessage = null;
        try
        {
            var res = await _layoutClient.DeleteZoneAsync(Layout.Id, SelectedZone.Id, Layout.Revision);
            if (res?.Success == true && res.Data != null)
            {
                StatusMessage = $"Đã xóa khu vực {SelectedZone.Code}.";
                SelectedZone = null;
                LayoutMutated?.Invoke();
            }
            else
            {
                ErrorMessage = res?.Error?.Message ?? "Không thể xóa khu vực.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    public void SelectShelf(ShelfModel? shelf)
    {
        SelectedShelf = shelf;
    }

    [RelayCommand]
    public void DeselectShelf()
    {
        SelectedShelf = null;
    }

    [RelayCommand]
    public async Task MoveSelectedShelfAsync(string direction)
    {
        if (Layout == null || SelectedShelf == null) return;

        int newX = SelectedShelf.PositionX;
        int newY = SelectedShelf.PositionY;

        switch (direction)
        {
            case "Left":
                newX = Math.Max(0, newX - 1);
                break;
            case "Right":
                newX = Math.Min(Layout.GridWidth - SelectedShelf.Width, newX + 1);
                break;
            case "Up":
                newY = Math.Max(0, newY - 1);
                break;
            case "Down":
                newY = Math.Min(Layout.GridHeight - SelectedShelf.Height, newY + 1);
                break;
        }

        if (newX == SelectedShelf.PositionX && newY == SelectedShelf.PositionY) return;

        await UpdateShelfPositionAsync(SelectedShelf.Id, newX, newY);
    }

    public async Task UpdateShelfPositionAsync(Guid shelfId, int newX, int newY)
    {
        if (Layout == null) return;
        var shelf = Layout.Shelves.FirstOrDefault(s => s.Id == shelfId);
        if (shelf == null) return;

        IsSaving = true;
        ErrorMessage = null;
        try
        {
            var req = new UpdateShelfRequest(
                shelf.ZoneId,
                shelf.Code,
                shelf.Name,
                newX,
                newY,
                shelf.Width,
                shelf.Height,
                shelf.Orientation,
                shelf.Status,
                Layout.Revision);

            var res = await _layoutClient.UpdateShelfAsync(Layout.Id, shelfId, req);
            if (res?.Success == true && res.Data != null)
            {
                shelf.PositionX = res.Data.PositionX;
                shelf.PositionY = res.Data.PositionY;
                StatusMessage = $"Đã di chuyển kệ {shelf.Code} đến tọa độ ({shelf.PositionX}, {shelf.PositionY}).";
                LayoutMutated?.Invoke();
            }
            else
            {
                ErrorMessage = res?.Error?.Message ?? "Không thể di chuyển kệ hàng.";
                LayoutMutated?.Invoke();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            LayoutMutated?.Invoke();
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    public async Task DeleteSelectedShelfAsync()
    {
        if (Layout == null || SelectedShelf == null) return;

        IsSaving = true;
        ErrorMessage = null;
        try
        {
            var res = await _layoutClient.DeleteShelfAsync(Layout.Id, SelectedShelf.Id, Layout.Revision);
            if (res?.Success == true && res.Data != null)
            {
                StatusMessage = $"Đã xóa kệ hàng {SelectedShelf.Code}.";
                SelectedShelf = null;
                LayoutMutated?.Invoke();
            }
            else
            {
                ErrorMessage = res?.Error?.Message ?? "Không thể xóa kệ hàng.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    public void SetBrush(string brush)
    {
        SelectedBrush = brush;
    }

    public void PaintCell(int x, int y)
    {
        if (Layout == null || SelectedBrush == "Select") return;

        var cell = Layout.Cells.FirstOrDefault(c => c.X == x && c.Y == y);
        if (cell != null && cell.CellType != SelectedBrush)
        {
            // Record old state for undo (only the first change per cell per stroke)
            if (_currentStroke != null && !_currentStroke.Any(e => e.X == x && e.Y == y))
            {
                _currentStroke.Add((x, y, cell.CellType));
            }

            cell.CellType = SelectedBrush;
            _pendingCellChanges[(x, y)] = SelectedBrush;
            IsDirty = true;
        }
    }

    [RelayCommand]
    public async Task SaveCellChangesAsync()
    {
        if (Layout == null || _pendingCellChanges.Count == 0) return;

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            var cellRequests = _pendingCellChanges.Select(kv => new LayoutCellChangeRequest(
                kv.Key.X,
                kv.Key.Y,
                kv.Value,
                1)).ToList();

            var req = new UpdateLayoutCellsRequest(cellRequests, Layout.Revision);
            var res = await _layoutClient.UpdateCellsAsync(Layout.Id, req);

            if (res?.Success == true && res.Data != null)
            {
                Layout.Revision = res.Data.Revision;
                _pendingCellChanges.Clear();
                IsDirty = false;
                StatusMessage = "Đã lưu thay đổi ô lưới thành công.";
                LayoutMutated?.Invoke();
            }
            else
            {
                ErrorMessage = res?.Error?.Message ?? "Không thể lưu thay đổi ô lưới.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    public async Task AddShelfQuickAsync()
    {
        if (Layout == null) return;

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            int index = Layout.Shelves.Count + 1;
            string code = $"K{index:D2}";
            string name = $"Kệ hàng {index}";

            // Find an empty spot
            int posX = 2 + (Layout.Shelves.Count % 5) * 4;
            int posY = 2 + (Layout.Shelves.Count / 5) * 5;
            if (posX + 3 > Layout.GridWidth) posX = 1;
            if (posY + 2 > Layout.GridHeight) posY = 1;

            var req = new CreateShelfRequest(
                null,
                code,
                name,
                posX,
                posY,
                3,
                2,
                "North",
                Layout.Revision);

            var res = await _layoutClient.CreateShelfAsync(Layout.Id, req);
            if (res?.Success == true && res.Data != null)
            {
                StatusMessage = $"Đã thêm {name} ({code})!";
                LayoutMutated?.Invoke();
            }
            else
            {
                ErrorMessage = res?.Error?.Message ?? "Không thể thêm kệ hàng.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    public async Task AddZoneQuickAsync()
    {
        if (Layout == null) return;

        IsSaving = true;
        ErrorMessage = null;

        try
        {
            int index = Layout.Zones.Count + 1;
            string code = $"Z{index}";
            string name = $"Khu vực {index}";
            string[] colors = ["#3B82F6", "#10B981", "#F59E0B", "#8B5CF6", "#EC4899"];
            string color = colors[(index - 1) % colors.Length];

            int posX = 1 + (index - 1) * 6;
            int posY = 1;
            if (posX + 5 > Layout.GridWidth) posX = 1;

            var req = new CreateZoneRequest(
                code,
                name,
                color,
                posX,
                posY,
                6,
                6,
                Layout.Revision);

            var res = await _layoutClient.CreateZoneAsync(Layout.Id, req);
            if (res?.Success == true && res.Data != null)
            {
                StatusMessage = $"Đã thêm {name} ({code})!";
                LayoutMutated?.Invoke();
            }
            else
            {
                ErrorMessage = res?.Error?.Message ?? "Không thể thêm khu vực.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }
}
