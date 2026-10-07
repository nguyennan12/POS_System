using System.Windows;
using System.Windows.Media.Animation;

namespace POS.WinUI.Views.Shell;

public partial class SplashScreenWindow : Window
{
  public SplashScreenWindow()
  {
    InitializeComponent();
  }


  /// Cập nhật text trạng thái hiển thị trên Splash Screen một cách an toàn trên UI Thread.
  /// </summary>
  public void UpdateStatus(string status)
  {
    if (Dispatcher.CheckAccess())
    {
      TxtStatus.Text = status;
    }
    else
    {
      Dispatcher.InvokeAsync(() => TxtStatus.Text = status);
    }
  }


  /// Hoạt cảnh mờ dần (Fade-out) trong 200ms và đóng cửa sổ mượt mà.
  /// </summary>
  public async Task FadeOutAndCloseAsync(int durationMs = 200)
  {
    var tcs = new TaskCompletionSource();

    await Dispatcher.InvokeAsync(() =>
    {
      var fadeOut = new DoubleAnimation
      {
        From = 1.0,
        To = 0.0,
        Duration = TimeSpan.FromMilliseconds(durationMs),
        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
      };

      fadeOut.Completed += (s, e) =>
          {
          Close();
          tcs.TrySetResult();
        };

      BeginAnimation(OpacityProperty, fadeOut);
    });

    await tcs.Task;
  }
}
