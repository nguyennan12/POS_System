using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace POS.WinUI.Views.Cashier.Components;

public partial class PosCategoryBar : UserControl
{
  private Point _mouseStartPoint;
  private double _startHorizontalOffset;
  private bool _isMouseDown;
  private bool _isDragging;
  private DispatcherTimer? _scrollAnimTimer;
  private double _targetScrollOffset;

  public PosCategoryBar()
  {
    InitializeComponent();
  }

  private void OnCategoryBarLoaded(object sender, RoutedEventArgs e)
  {
    UpdateArrowButtonStates();
  }

  private void OnScrollLeftClick(object sender, RoutedEventArgs e)
  {
    AnimateScroll(CategoryScrollViewer.HorizontalOffset - 180);
  }

  private void OnScrollRightClick(object sender, RoutedEventArgs e)
  {
    AnimateScroll(CategoryScrollViewer.HorizontalOffset + 180);
  }

  private void OnCategoryPreviewMouseWheel(object sender, MouseWheelEventArgs e)
  {
    if (e.Delta != 0)
    {
      // Lăn chuột qua vùng danh mục -> cuộn ngang mượt mà (lăn xuống/qua phải, lăn lên/qua trái)
      double step = e.Delta > 0 ? -140 : 140;
      AnimateScroll(CategoryScrollViewer.HorizontalOffset + step);
      e.Handled = true;
    }
  }


  /// Cuộn mượt mà có gia tốc 60fps khi nhấn nút mũi tên hoặc lăn chuột
  /// </summary>
  private void AnimateScroll(double target)
  {
    target = Math.Max(0, Math.Min(CategoryScrollViewer.ScrollableWidth, target));
    _targetScrollOffset = target;

    _scrollAnimTimer?.Stop();
    _scrollAnimTimer = new DispatcherTimer
    {
      Interval = TimeSpan.FromMilliseconds(16)
    };

    _scrollAnimTimer.Tick += (s, e) =>
    {
      var current = CategoryScrollViewer.HorizontalOffset;
      var diff = _targetScrollOffset - current;
      if (Math.Abs(diff) < 2)
      {
        CategoryScrollViewer.ScrollToHorizontalOffset(_targetScrollOffset);
        _scrollAnimTimer.Stop();
        UpdateArrowButtonStates();
        return;
      }

      var next = current + diff * 0.35;
      CategoryScrollViewer.ScrollToHorizontalOffset(next);
      UpdateArrowButtonStates();
    };
    _scrollAnimTimer.Start();
  }

  // ── Kéo chuột ngang & Vuốt lướt danh mục (Mouse Drag Scroll) ──
  private void OnCategoryPreviewMouseDown(object sender, MouseButtonEventArgs e)
  {
    if (e.LeftButton == MouseButtonState.Pressed)
    {
      _mouseStartPoint = e.GetPosition(CategoryScrollViewer);
      _startHorizontalOffset = CategoryScrollViewer.HorizontalOffset;
      _isMouseDown = true;
      _isDragging = false;
    }
  }

  private void OnCategoryPreviewMouseMove(object sender, MouseEventArgs e)
  {
    if (_isMouseDown && e.LeftButton == MouseButtonState.Pressed)
    {
      var currentPoint = e.GetPosition(CategoryScrollViewer);
      var deltaX = currentPoint.X - _mouseStartPoint.X;

      // Ngưỡng kéo để phân biệt giữa Click chọn danh mục và Kéo cuộn danh sách
      if (!_isDragging && Math.Abs(deltaX) > 6)
      {
        _isDragging = true;
        CategoryScrollViewer.CaptureMouse();
      }

      if (_isDragging)
      {
        var targetOffset = Math.Max(0, Math.Min(CategoryScrollViewer.ScrollableWidth, _startHorizontalOffset - deltaX));
        CategoryScrollViewer.ScrollToHorizontalOffset(targetOffset);
        UpdateArrowButtonStates();
      }
    }
  }

  private void OnCategoryPreviewMouseUp(object sender, MouseButtonEventArgs e)
  {
    if (_isDragging)
    {
      CategoryScrollViewer.ReleaseMouseCapture();
      _isDragging = false;
      _isMouseDown = false;
      e.Handled = true; // Ngăn không kích hoạt chọn danh mục khi đang kéo lướt
      UpdateArrowButtonStates();
      return;
    }

    _isMouseDown = false;
    _isDragging = false;
  }

  private void OnCategoryScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
  {
    UpdateArrowButtonStates();
  }


  /// Cập nhật độ mờ và trạng thái tương tác của 2 nút mũi tên ở 2 đầu
  /// </summary>
  private void UpdateArrowButtonStates()
  {
    if (BtnScrollLeft == null || BtnScrollRight == null || CategoryScrollViewer == null) return;

    if (CategoryScrollViewer.ScrollableWidth > 0)
    {
      bool canScrollLeft = CategoryScrollViewer.HorizontalOffset > 2;
      BtnScrollLeft.Opacity = canScrollLeft ? 1.0 : 0.4;
      BtnScrollLeft.IsEnabled = canScrollLeft;

      bool canScrollRight = CategoryScrollViewer.HorizontalOffset < CategoryScrollViewer.ScrollableWidth - 2;
      BtnScrollRight.Opacity = canScrollRight ? 1.0 : 0.4;
      BtnScrollRight.IsEnabled = canScrollRight;
    }
    else
    {
      BtnScrollLeft.Opacity = 0.4;
      BtnScrollLeft.IsEnabled = false;
      BtnScrollRight.Opacity = 0.4;
      BtnScrollRight.IsEnabled = false;
    }
  }
}
