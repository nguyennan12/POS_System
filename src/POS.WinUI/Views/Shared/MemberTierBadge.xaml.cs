using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace POS.WinUI.Views.Shared;

public partial class MemberTierBadge : UserControl
{
    public static readonly DependencyProperty TierNameProperty =
        DependencyProperty.Register(
            nameof(TierName),
            typeof(string),
            typeof(MemberTierBadge),
            new PropertyMetadata("Normal", OnTierChanged));

    public string? TierName
    {
        get => (string?)GetValue(TierNameProperty);
        set => SetValue(TierNameProperty, value);
    }

    public static readonly DependencyProperty ShowIconProperty =
        DependencyProperty.Register(
            nameof(ShowIcon),
            typeof(bool),
            typeof(MemberTierBadge),
            new PropertyMetadata(true, OnTierChanged));

    public bool ShowIcon
    {
        get => (bool)GetValue(ShowIconProperty);
        set => SetValue(ShowIconProperty, value);
    }

    public MemberTierBadge()
    {
        InitializeComponent();
        UpdateBadgeStyle();
    }

    private static void OnTierChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (MemberTierBadge)d;
        ctrl.UpdateBadgeStyle();
    }

    public void UpdateBadgeStyle()
    {
        var rawTier = (TierName ?? string.Empty).Trim();
        var lower = rawTier.ToLowerInvariant();

        TierIcon.Visibility = ShowIcon ? Visibility.Visible : Visibility.Collapsed;

        // 1. Hạng VIP / Kim Cương / Diamond / Platinum (Gradient Tím Hoàng Gia)
        if (lower.Contains("vip") || lower.Contains("diamond") || lower.Contains("kim") || lower.Contains("plat"))
        {
            TierText.Text = string.IsNullOrWhiteSpace(rawTier) ? "Hạng VIP" : rawTier;
            BadgeBorder.Background = CreateGradient("#FAF5FF", "#E9D5FF");
            BadgeBorder.BorderBrush = CreateGradient("#C084FC", "#9333EA");
            TierText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B21A8"));
            TierIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7E22CE"));
            TierIcon.Symbol = SymbolRegular.Premium24;
        }
        // 2. Hạng Vàng / Gold (Gradient Hoàng Kim Rực Rỡ)
        else if (lower.Contains("gold") || lower.Contains("vàng") || lower.Contains("vang"))
        {
            TierText.Text = string.IsNullOrWhiteSpace(rawTier) ? "Hạng Vàng" : rawTier;
            BadgeBorder.Background = CreateGradient("#FFFBEB", "#FEF08A");
            BadgeBorder.BorderBrush = CreateGradient("#FBBF24", "#D97706");
            TierText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#854D0E"));
            TierIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
            TierIcon.Symbol = SymbolRegular.Trophy24;
        }
        // 3. Hạng Bạc / Silver (Gradient Xanh Da Trời Nhẹ)
        else if (lower.Contains("silver") || lower.Contains("bạc") || lower.Contains("bac"))
        {
            TierText.Text = string.IsNullOrWhiteSpace(rawTier) ? "Hạng Bạc" : rawTier;
            BadgeBorder.Background = Create3StopGradient("#F0F9FF", "#E0F2FE", "#BAE6FD");
            BadgeBorder.BorderBrush = CreateGradient("#7DD3FC", "#38BDF8");
            TierText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0369A1"));
            TierIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
            TierIcon.Symbol = SymbolRegular.RibbonStar24;
        }
        // 4. Hạng Chuẩn / Normal / Thành viên (Gradient Trung Tính Tinh Tế)
        else
        {
            TierText.Text = string.IsNullOrWhiteSpace(rawTier) ? "Thành viên" : rawTier;
            BadgeBorder.Background = CreateGradient("#F8FAFC", "#F1F5F9");
            BadgeBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1"));
            TierText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
            TierIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
            TierIcon.Symbol = SymbolRegular.PersonCircle24;
        }
    }

    private static LinearGradientBrush CreateGradient(string hexStart, string hexEnd)
    {
        var colorStart = (Color)ColorConverter.ConvertFromString(hexStart);
        var colorEnd = (Color)ColorConverter.ConvertFromString(hexEnd);
        return new LinearGradientBrush(colorStart, colorEnd, new Point(0, 0), new Point(1, 1));
    }

    private static LinearGradientBrush Create3StopGradient(string hexStart, string hexMid, string hexEnd)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(hexStart), 0.0));
        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(hexMid), 0.5));
        brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(hexEnd), 1.0));
        return brush;
    }
}
