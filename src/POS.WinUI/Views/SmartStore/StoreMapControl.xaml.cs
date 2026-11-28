using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using POS.WinUI.ViewModels.SmartStore;
using POS.WinUI.ViewModels.SmartStore.Models;

namespace POS.WinUI.Views.SmartStore;

public partial class StoreMapControl : UserControl
{
    private Point _mouseDownPos;
    private Point _lastDragPoint;
    private bool _isMouseDown;
    private bool _isPanning;
    private MouseButton? _pressedButton;
    private const double DragThreshold = 3.0;

    public StoreMapControl()
    {
        InitializeComponent();
    }

    private void OnViewportPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (IsInsideZoomToolbar(e.OriginalSource as DependencyObject))
            return;

        ViewportGrid.Focus();
        _pressedButton = e.ChangedButton;

        if (e.LeftButton == MouseButtonState.Pressed ||
            e.MiddleButton == MouseButtonState.Pressed ||
            e.RightButton == MouseButtonState.Pressed)
        {
            _isMouseDown = true;
            _isPanning = false;
            _mouseDownPos = e.GetPosition(this);
            _lastDragPoint = _mouseDownPos;

            // Begin a paint stroke when left-clicking with a brush active
            if (e.LeftButton == MouseButtonState.Pressed && IsPaintBrushActive() && !Keyboard.IsKeyDown(Key.Space))
            {
                FindParentStoreLayoutViewModel()?.EditorVm.BeginStroke();
            }

            // Middle click, Right click, or Space+Left immediately starts panning
            if (e.MiddleButton == MouseButtonState.Pressed ||
                e.RightButton == MouseButtonState.Pressed ||
                (e.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.Space)))
            {
                StartPanning();
                e.Handled = true;
            }
        }
    }

    private void OnViewportPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_isMouseDown && DataContext is StoreMapViewModel vm)
        {
            // If dragging with Left mouse while painting brush is active and not pressing Space,
            // do not hijack with panning (allow painting cells)
            if (_pressedButton == MouseButton.Left && IsPaintBrushActive() && !Keyboard.IsKeyDown(Key.Space))
            {
                return;
            }

            var currentPos = e.GetPosition(this);

            if (!_isPanning)
            {
                var delta = currentPos - _mouseDownPos;
                if (Math.Abs(delta.X) >= DragThreshold || Math.Abs(delta.Y) >= DragThreshold)
                {
                    StartPanning();
                }
            }

            if (_isPanning)
            {
                var deltaX = currentPos.X - _lastDragPoint.X;
                var deltaY = currentPos.Y - _lastDragPoint.Y;

                vm.PanX += deltaX;
                vm.PanY += deltaY;

                _lastDragPoint = currentPos;
                e.Handled = true;
            }
        }
    }

    private void OnViewportPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isMouseDown)
        {
            bool wasPanning = _isPanning;

            // End paint stroke before releasing
            if (e.ChangedButton == MouseButton.Left && !wasPanning)
            {
                FindParentStoreLayoutViewModel()?.EditorVm.EndStroke();
            }

            _isMouseDown = false;
            _isPanning = false;
            _pressedButton = null;
            ViewportGrid.ReleaseMouseCapture();
            Cursor = Cursors.Arrow;

            if (wasPanning)
            {
                // Suppress click event on child items if user was panning
                e.Handled = true;
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                e.Handled = true;
            }
        }
    }

    private void OnViewportLostMouseCapture(object sender, MouseEventArgs e)
    {
        // End any ongoing paint stroke
        if (_isMouseDown && !_isPanning)
        {
            FindParentStoreLayoutViewModel()?.EditorVm.EndStroke();
        }
        _isMouseDown = false;
        _isPanning = false;
        _pressedButton = null;
        Cursor = Cursors.Arrow;
    }

    private void StartPanning()
    {
        _isPanning = true;
        ViewportGrid.CaptureMouse();
        Cursor = Cursors.SizeAll;
    }

    private bool IsInsideZoomToolbar(DependencyObject? element)
    {
        while (element != null)
        {
            if (element == ZoomToolbar) return true;
            element = VisualTreeHelper.GetParent(element);
        }
        return false;
    }

    private void OnViewportMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is StoreMapViewModel vm)
        {
            if (e.Delta > 0)
            {
                vm.ZoomIn();
            }
            else
            {
                vm.ZoomOut();
            }
            e.Handled = true;
        }
    }

    private void OnViewportKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is StoreMapViewModel vm)
        {
            switch (e.Key)
            {
                case Key.Left:
                    vm.PanX += 30;
                    e.Handled = true;
                    break;
                case Key.Right:
                    vm.PanX -= 30;
                    e.Handled = true;
                    break;
                case Key.Up:
                    vm.PanY += 30;
                    e.Handled = true;
                    break;
                case Key.Down:
                    vm.PanY -= 30;
                    e.Handled = true;
                    break;
                case Key.OemPlus:
                case Key.Add:
                    vm.ZoomIn();
                    e.Handled = true;
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    vm.ZoomOut();
                    e.Handled = true;
                    break;
                case Key.D0:
                case Key.NumPad0:
                    vm.ResetZoom();
                    e.Handled = true;
                    break;
                case Key.Z when (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                    // Ctrl+Z → Undo last stroke
                    var layoutVmForUndo = FindParentStoreLayoutViewModel();
                    if (layoutVmForUndo?.EditorVm.CanUndo == true)
                    {
                        layoutVmForUndo.EditorVm.UndoLastStrokeCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;
            }
        }
    }

    private CanvasShelfVisual? _draggingShelf;
    private Point _shelfDragStartPos;
    private double _shelfOrigLeft;
    private double _shelfOrigTop;
    private bool _isShelfDragging;

    private void OnShelfMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is CanvasShelfVisual shelfVisual)
        {
            var layoutVm = FindParentStoreLayoutViewModel();
            if (layoutVm is { IsDesignMode: true })
            {
                if (!layoutVm.EditorVm.CanEdit) return;

                // Select this shelf in EditorVm
                var shelfModel = layoutVm.CurrentLayout?.Shelves.FirstOrDefault(s => s.Id == shelfVisual.Id);
                layoutVm.EditorVm.SelectShelf(shelfModel);

                // Update isSelected flag on MapVm
                if (DataContext is StoreMapViewModel mapVm)
                {
                    foreach (var s in mapVm.Shelves)
                    {
                        s.IsSelected = (s.Id == shelfVisual.Id);
                    }
                }

                _draggingShelf = shelfVisual;
                _isShelfDragging = false;
                _shelfDragStartPos = e.GetPosition(ViewportGrid);
                _shelfOrigLeft = shelfVisual.CanvasLeft;
                _shelfOrigTop = shelfVisual.CanvasTop;

                fe.CaptureMouse();
                e.Handled = true;
            }
            else if (DataContext is StoreMapViewModel vm)
            {
                vm.SelectShelf(shelfVisual);
                e.Handled = true;
            }
        }
    }

    private void OnShelfMouseMove(object sender, MouseEventArgs e)
    {
        if (_draggingShelf != null && sender is FrameworkElement fe && fe.IsMouseCaptured)
        {
            var currentPos = e.GetPosition(ViewportGrid);
            var deltaX = currentPos.X - _shelfDragStartPos.X;
            var deltaY = currentPos.Y - _shelfDragStartPos.Y;

            if (!_isShelfDragging && (Math.Abs(deltaX) > 3 || Math.Abs(deltaY) > 3))
            {
                _isShelfDragging = true;
            }

            if (_isShelfDragging && DataContext is StoreMapViewModel mapVm)
            {
                double zoom = mapVm.Zoom > 0 ? mapVm.Zoom : 1.0;
                double newLeft = _shelfOrigLeft + (deltaX / zoom);
                double newTop = _shelfOrigTop + (deltaY / zoom);

                newLeft = Math.Max(0, Math.Min(mapVm.CanvasWidth - _draggingShelf.CanvasWidth, newLeft));
                newTop = Math.Max(0, Math.Min(mapVm.CanvasHeight - _draggingShelf.CanvasHeight, newTop));

                _draggingShelf.CanvasLeft = newLeft;
                _draggingShelf.CanvasTop = newTop;
                e.Handled = true;
            }
        }
    }

    private async void OnShelfMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggingShelf != null && sender is FrameworkElement fe)
        {
            fe.ReleaseMouseCapture();
            var shelf = _draggingShelf;
            bool wasDragging = _isShelfDragging;
            _draggingShelf = null;
            _isShelfDragging = false;

            if (wasDragging)
            {
                double cellSize = StoreMapViewModel.CellSize;
                int newGridX = (int)Math.Round(shelf.CanvasLeft / cellSize);
                int newGridY = (int)Math.Round(shelf.CanvasTop / cellSize);

                var layoutVm = FindParentStoreLayoutViewModel();
                if (layoutVm != null)
                {
                    int maxGridX = (layoutVm.CurrentLayout?.GridWidth ?? 20) - shelf.GridWidth;
                    int maxGridY = (layoutVm.CurrentLayout?.GridHeight ?? 15) - shelf.GridHeight;
                    newGridX = Math.Max(0, Math.Min(maxGridX, newGridX));
                    newGridY = Math.Max(0, Math.Min(maxGridY, newGridY));

                    shelf.GridX = newGridX;
                    shelf.GridY = newGridY;
                    shelf.CanvasLeft = newGridX * cellSize;
                    shelf.CanvasTop = newGridY * cellSize;

                    await layoutVm.EditorVm.UpdateShelfPositionAsync(shelf.Id, newGridX, newGridY);
                }
            }
            e.Handled = true;
        }
    }

    private void OnShelfClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: CanvasShelfVisual shelfVisual } && DataContext is StoreMapViewModel vm)
        {
            vm.SelectShelf(shelfVisual);
            e.Handled = true;
        }
    }

    private CanvasZoneVisual? _draggingZone;
    private Point _zoneDragStartPos;
    private double _zoneOrigLeft;
    private double _zoneOrigTop;
    private bool _isZoneDragging;

    private void OnZoneMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is CanvasZoneVisual zoneVisual)
        {
            var layoutVm = FindParentStoreLayoutViewModel();
            if (layoutVm is { IsDesignMode: true })
            {
                if (!layoutVm.EditorVm.CanEdit) return;

                // Select this zone in EditorVm
                var zoneModel = layoutVm.CurrentLayout?.Zones.FirstOrDefault(z => z.Id == zoneVisual.Id);
                layoutVm.EditorVm.SelectZone(zoneModel);

                // Update isSelected flag on MapVm
                if (DataContext is StoreMapViewModel mapVm)
                {
                    foreach (var z in mapVm.Zones)
                    {
                        z.IsSelected = (z.Id == zoneVisual.Id);
                    }
                }

                _draggingZone = zoneVisual;
                _isZoneDragging = false;
                _zoneDragStartPos = e.GetPosition(ViewportGrid);
                _zoneOrigLeft = zoneVisual.CanvasLeft;
                _zoneOrigTop = zoneVisual.CanvasTop;

                fe.CaptureMouse();
                e.Handled = true;
            }
        }
    }

    private void OnZoneMouseMove(object sender, MouseEventArgs e)
    {
        if (_draggingZone != null && sender is FrameworkElement fe && fe.IsMouseCaptured)
        {
            var currentPos = e.GetPosition(ViewportGrid);
            var deltaX = currentPos.X - _zoneDragStartPos.X;
            var deltaY = currentPos.Y - _zoneDragStartPos.Y;

            if (!_isZoneDragging && (Math.Abs(deltaX) > 3 || Math.Abs(deltaY) > 3))
            {
                _isZoneDragging = true;
            }

            if (_isZoneDragging && DataContext is StoreMapViewModel mapVm)
            {
                double zoom = mapVm.Zoom > 0 ? mapVm.Zoom : 1.0;
                double newLeft = _zoneOrigLeft + (deltaX / zoom);
                double newTop = _zoneOrigTop + (deltaY / zoom);

                newLeft = Math.Max(0, Math.Min(mapVm.CanvasWidth - _draggingZone.CanvasWidth, newLeft));
                newTop = Math.Max(0, Math.Min(mapVm.CanvasHeight - _draggingZone.CanvasHeight, newTop));

                _draggingZone.CanvasLeft = newLeft;
                _draggingZone.CanvasTop = newTop;
                e.Handled = true;
            }
        }
    }

    private async void OnZoneMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggingZone != null && sender is FrameworkElement fe)
        {
            fe.ReleaseMouseCapture();
            var zone = _draggingZone;
            bool wasDragging = _isZoneDragging;
            _draggingZone = null;
            _isZoneDragging = false;

            if (wasDragging)
            {
                double cellSize = StoreMapViewModel.CellSize;
                int newGridX = (int)Math.Round(zone.CanvasLeft / cellSize);
                int newGridY = (int)Math.Round(zone.CanvasTop / cellSize);

                var layoutVm = FindParentStoreLayoutViewModel();
                if (layoutVm != null)
                {
                    int maxGridX = (layoutVm.CurrentLayout?.GridWidth ?? 20) - zone.GridWidth;
                    int maxGridY = (layoutVm.CurrentLayout?.GridHeight ?? 15) - zone.GridHeight;
                    newGridX = Math.Max(0, Math.Min(maxGridX, newGridX));
                    newGridY = Math.Max(0, Math.Min(maxGridY, newGridY));

                    zone.GridX = newGridX;
                    zone.GridY = newGridY;
                    zone.CanvasLeft = newGridX * cellSize;
                    zone.CanvasTop = newGridY * cellSize;

                    await layoutVm.EditorVm.UpdateZonePositionAsync(zone.Id, newGridX, newGridY);
                }
            }
            e.Handled = true;
        }
    }

    private void OnCellClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: CanvasCellVisual cellVisual })
        {
            if (FindParentStoreLayoutViewModel() is { IsDesignMode: true } layoutVm)
            {
                if (!layoutVm.EditorVm.CanEdit)
                {
                    layoutVm.EditorVm.StatusMessage = "Mặt bằng đang hoạt động được bảo vệ ở chế độ Chỉ đọc. Bấm 'Nhân bản thành Bản Thảo để sửa' ở bảng bên phải để bắt đầu thiết kế.";
                    return;
                }

                if (layoutVm.EditorVm.SelectedBrush != "Select")
                {
                    layoutVm.EditorVm.PaintCell(cellVisual.X, cellVisual.Y);
                    cellVisual.CellType = layoutVm.EditorVm.SelectedBrush;
                    e.Handled = true;
                }
                else
                {
                    layoutVm.EditorVm.StatusMessage = $"Ô ({cellVisual.X}, {cellVisual.Y}) - Loại: {cellVisual.CellType}. Chọn một loại bút vẽ (Lối đi, Vật cản, Cửa ra vào, Thu ngân) để tô màu.";
                }
            }
        }
    }

    private void OnCellMouseEnter(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed &&
            FindParentStoreLayoutViewModel() is { IsDesignMode: true } layoutVm &&
            layoutVm.EditorVm.CanEdit &&
            layoutVm.EditorVm.SelectedBrush != "Select" &&
            sender is FrameworkElement { DataContext: CanvasCellVisual cellVisual })
        {
            layoutVm.EditorVm.PaintCell(cellVisual.X, cellVisual.Y);
            cellVisual.CellType = layoutVm.EditorVm.SelectedBrush;
        }
    }

    private bool IsPaintBrushActive()
    {
        return FindParentStoreLayoutViewModel() is { IsDesignMode: true } layoutVm
            && layoutVm.EditorVm.CanEdit
            && layoutVm.EditorVm.SelectedBrush != "Select";
    }

    private StoreLayoutViewModel? FindParentStoreLayoutViewModel()
    {
        DependencyObject current = this;
        while (current != null)
        {
            if (current is FrameworkElement fe && fe.DataContext is StoreLayoutViewModel lvm)
            {
                return lvm;
            }
            current = LogicalTreeHelper.GetParent(current) ?? VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
