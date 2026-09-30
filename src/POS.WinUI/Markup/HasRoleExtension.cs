using System.Windows;
using System.Windows.Markup;
using POS.WinUI.Services;

namespace POS.WinUI.Markup;

[MarkupExtensionReturnType(typeof(object))]
public class HasRoleExtension : MarkupExtension
{
    public string? Roles { get; set; }
    public int MinLevel { get; set; } = 0;
    public bool Inverse { get; set; } = false;

    public HasRoleExtension() { }

    public HasRoleExtension(string roles)
    {
        Roles = roles;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var session = SessionService.Current;
        bool hasAccess = false;

        if (session != null && session.IsLoggedIn)
        {
            if (MinLevel > 0)
            {
                hasAccess = session.RoleLevel >= MinLevel;
            }
            else if (!string.IsNullOrWhiteSpace(Roles))
            {
                hasAccess = session.IsInRole(Roles);
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
