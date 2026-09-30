using System.Globalization;
using System.Windows;
using System.Windows.Data;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Converters;

public sealed class PermissionToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var session = SessionService.Current;
        var permissionCode = parameter?.ToString() ?? value?.ToString();

        bool hasAccess = session != null && session.IsLoggedIn && !string.IsNullOrWhiteSpace(permissionCode) && session.HasPermission(permissionCode);

        if (Invert)
        {
            hasAccess = !hasAccess;
        }

        if (targetType == typeof(bool) || targetType == typeof(bool?))
        {
            return hasAccess;
        }

        return hasAccess ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
