using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace POS.WinUI.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; } = false;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool boolVal = false;
        if (value is bool b)
        {
            boolVal = b;
        }

        if (Invert) boolVal = !boolVal;
        return boolVal ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            bool isVisible = visibility == Visibility.Visible;
            return Invert ? !isVisible : isVisible;
        }
        return false;
    }
}
