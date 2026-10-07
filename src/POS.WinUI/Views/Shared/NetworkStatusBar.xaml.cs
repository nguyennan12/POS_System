using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace POS.WinUI.Views.Shared;

///  
/// Thanh trạng thái kết nối server — tái sử dụng ở bất kỳ màn hình nào
/// </summary>
public partial class NetworkStatusBar : UserControl
{
  // ── IsConnected ───────────────────────────────────────────────
  public static readonly DependencyProperty IsConnectedProperty =
      DependencyProperty.Register(nameof(IsConnected), typeof(bool), typeof(NetworkStatusBar),
          new PropertyMetadata(true));

  public bool IsConnected
  {
    get => (bool)GetValue(IsConnectedProperty);
    set => SetValue(IsConnectedProperty, value);
  }

  // ── StatusText ────────────────────────────────────────────────
  public static readonly DependencyProperty StatusTextProperty =
      DependencyProperty.Register(nameof(StatusText), typeof(string), typeof(NetworkStatusBar),
          new PropertyMetadata("Đang kiểm tra..."));

  public string StatusText
  {
    get => (string)GetValue(StatusTextProperty);
    set => SetValue(StatusTextProperty, value);
  }

  // ── RetryCommand ──────────────────────────────────────────────
  public static readonly DependencyProperty RetryCommandProperty =
      DependencyProperty.Register(nameof(RetryCommand), typeof(ICommand), typeof(NetworkStatusBar),
          new PropertyMetadata(null));

  public ICommand? RetryCommand
  {
    get => (ICommand?)GetValue(RetryCommandProperty);
    set => SetValue(RetryCommandProperty, value);
  }

  public NetworkStatusBar()
  {
    InitializeComponent();
  }
}
