using System.Windows;
using System.Windows.Markup;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Resources.Markup;

[MarkupExtensionReturnType(typeof(object))]
public class HasPermissionExtension : MarkupExtension
{
    public string? Permission { get; set; }
    public bool Inverse { get; set; } = false;

    public HasPermissionExtension() { }

    public HasPermissionExtension(string permission)
    {
        Permission = permission;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var session = SessionService.Current;
        bool hasAccess = false;

        if (session != null && session.IsLoggedIn)
        {
            if (!string.IsNullOrWhiteSpace(Permission))
            {
                hasAccess = session.HasPermission(Permission);
            }
        }

        if (Inverse)
        {
            hasAccess = !hasAccess;
        }

        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget provideValueTarget)
        {
            if (provideValueTarget.TargetProperty is DependencyProperty dp)
            {
                if (dp.PropertyType == typeof(Visibility))
                {
                    return hasAccess ? Visibility.Visible : Visibility.Collapsed;
                }
                if (dp.PropertyType == typeof(bool) || dp.PropertyType == typeof(bool?))
                {
                    return hasAccess;
                }
            }
        }

        return hasAccess ? Visibility.Visible : Visibility.Collapsed;
    }
}
