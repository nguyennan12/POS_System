using System;
using System.Windows;
using System.Windows.Controls;

namespace POS.WinUI.Views.Cashier;


public class AdaptiveCardPanel : Panel
{
    public static readonly DependencyProperty MinItemWidthProperty =
        DependencyProperty.Register(
            nameof(MinItemWidth),
            typeof(double),
            typeof(AdaptiveCardPanel),
            new FrameworkPropertyMetadata(150.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    public static readonly DependencyProperty ItemHeightProperty =
        DependencyProperty.Register(
            nameof(ItemHeight),
            typeof(double),
            typeof(AdaptiveCardPanel),
            new FrameworkPropertyMetadata(210.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    public static readonly DependencyProperty SpacingProperty =
        DependencyProperty.Register(
            nameof(Spacing),
            typeof(double),
            typeof(AdaptiveCardPanel),
            new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    public double MinItemWidth
    {
        get => (double)GetValue(MinItemWidthProperty);
        set => SetValue(MinItemWidthProperty, value);
    }

    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    private int _calculatedColumns = 1;
    private double _calculatedItemWidth = 150.0;

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = availableSize.Width;
        if (double.IsInfinity(width) || double.IsNaN(width) || width <= 0)
        {
            width = MinItemWidth;
        }

        double spacing = Spacing;
        double minItemWidth = Math.Max(50, MinItemWidth);

        // Tính số cột tối ưu có thể xếp vừa chiều ngang khả dụng
        int columns = (int)((width + spacing) / (minItemWidth + spacing));
        if (columns < 1)
        {
            columns = 1;
        }

        _calculatedColumns = columns;

        // Co giãn chiều rộng thẻ đều nhau để lấp đầy 100% chiều rộng, không để trống bên phải
        _calculatedItemWidth = (width - (columns - 1) * spacing) / columns;
        if (_calculatedItemWidth < 0)
        {
            _calculatedItemWidth = minItemWidth;
        }

        int count = InternalChildren.Count;
        int rows = count > 0 ? (count + columns - 1) / columns : 0;

        Size childConstraint = new Size(_calculatedItemWidth, ItemHeight);
        foreach (UIElement child in InternalChildren)
        {
            child?.Measure(childConstraint);
        }

        double totalHeight = rows > 0 ? (rows * ItemHeight + (rows - 1) * spacing) : 0;
        return new Size(width, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        int columns = _calculatedColumns;
        if (columns < 1)
        {
            columns = 1;
        }

        double spacing = Spacing;
        double itemWidth = _calculatedItemWidth;
        double itemHeight = ItemHeight;

        for (int i = 0; i < InternalChildren.Count; i++)
        {
            UIElement child = InternalChildren[i];
            if (child == null) continue;

            int row = i / columns;
            int col = i % columns;

            double x = col * (itemWidth + spacing);
            double y = row * (itemHeight + spacing);

            child.Arrange(new Rect(x, y, itemWidth, itemHeight));
        }

        return finalSize;
    }
}
