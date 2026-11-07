using System.Collections;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace POS.WinUI.Views.Shared;

public partial class IconFilter : UserControl
{
    public IconFilter() => InitializeComponent();
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(IconFilter));
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(object), typeof(IconFilter), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(IconFilter), new PropertyMetadata("Filter"));
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(SymbolRegular), typeof(IconFilter), new PropertyMetadata(SymbolRegular.Filter24));
    public static readonly DependencyProperty IsAppliedProperty = DependencyProperty.Register(nameof(IsApplied), typeof(bool), typeof(IconFilter));
    public IEnumerable? ItemsSource { get => (IEnumerable?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public object? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public SymbolRegular Icon { get => (SymbolRegular)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public bool IsApplied { get => (bool)GetValue(IsAppliedProperty); set => SetValue(IsAppliedProperty, value); }
}
