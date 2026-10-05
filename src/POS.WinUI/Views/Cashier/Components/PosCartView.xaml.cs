using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace POS.WinUI.Views.Cashier.Components;

public partial class PosCartView : UserControl
{
  public FrameworkElement CartTargetBadge => CartBadge;

  public PosCartView()
  {
    InitializeComponent();
  }


  /// Hiệu ứng nảy nhẹ biểu tượng giỏ hàng khi thẻ sản phẩm vừa bay tới đích
  /// </summary>
  public void PlayBounceAnimation()
  {
    if (CartBadge == null) return;

    var scale = new ScaleTransform(1.0, 1.0, CartBadge.ActualWidth / 2, CartBadge.ActualHeight / 2);
    CartBadge.RenderTransform = scale;

    var bounceAnim = new DoubleAnimationUsingKeyFrames
    {
      Duration = TimeSpan.FromMilliseconds(220)
    };
    bounceAnim.KeyFrames.Add(new SplineDoubleKeyFrame(1.28, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(90))));
    bounceAnim.KeyFrames.Add(new SplineDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(220))));

    scale.BeginAnimation(ScaleTransform.ScaleXProperty, bounceAnim);
    scale.BeginAnimation(ScaleTransform.ScaleYProperty, bounceAnim);
  }
}
