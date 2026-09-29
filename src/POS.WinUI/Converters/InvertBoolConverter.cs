using System.Globalization;
using System.Windows.Data;

namespace POS.WinUI.Converters;

/// <summary>Negates a bool — useful for IsEnabled="{Binding IsLoading, Converter={StaticResource InvertBool}}".</summary>
public class InvertBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : true;
}
