using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace POS.WinUI.Views.Auth.Components;

/// <summary>
/// Bàn phím số PIN và chỉ số hiển thị cho luồng xác thực đăng nhập.
/// </summary>
public partial class PinInput : UserControl
{
    // ── PinLength ────────────────────────────────────────────────
    public static readonly DependencyProperty PinLengthProperty =
        DependencyProperty.Register(nameof(PinLength), typeof(int), typeof(PinInput),
            new PropertyMetadata(0, OnPinLengthChanged));

    public int PinLength
    {
        get => (int)GetValue(PinLengthProperty);
        set => SetValue(PinLengthProperty, value);
    }

    // ── AddDigitCommand ───────────────────────────────────────────
    public static readonly DependencyProperty AddDigitCommandProperty =
        DependencyProperty.Register(nameof(AddDigitCommand), typeof(ICommand), typeof(PinInput),
            new PropertyMetadata(null));

    public ICommand? AddDigitCommand
    {
        get => (ICommand?)GetValue(AddDigitCommandProperty);
        set => SetValue(AddDigitCommandProperty, value);
    }

    // ── BackspaceCommand ──────────────────────────────────────────
    public static readonly DependencyProperty BackspaceCommandProperty =
        DependencyProperty.Register(nameof(BackspaceCommand), typeof(ICommand), typeof(PinInput),
            new PropertyMetadata(null));

    public ICommand? BackspaceCommand
    {
        get => (ICommand?)GetValue(BackspaceCommandProperty);
        set => SetValue(BackspaceCommandProperty, value);
    }

    // ── ClearCommand ──────────────────────────────────────────────
    public static readonly DependencyProperty ClearCommandProperty =
        DependencyProperty.Register(nameof(ClearCommand), typeof(ICommand), typeof(PinInput),
            new PropertyMetadata(null));

    public ICommand? ClearCommand
    {
        get => (ICommand?)GetValue(ClearCommandProperty);
        set => SetValue(ClearCommandProperty, value);
    }

    public PinInput()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Cập nhật trạng thái 6 chấm tròn khi PinLength thay đổi.
    /// Dùng Tag=true/false để kích hoạt PinDotStyle DataTrigger trong Styles.xaml.
    /// </summary>
    private static void OnPinLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (PinInput)d;
        int len = (int)e.NewValue;

        ctrl.Dot0.Tag = len > 0;
        ctrl.Dot1.Tag = len > 1;
        ctrl.Dot2.Tag = len > 2;
        ctrl.Dot3.Tag = len > 3;
        ctrl.Dot4.Tag = len > 4;
        ctrl.Dot5.Tag = len > 5;
    }
}
