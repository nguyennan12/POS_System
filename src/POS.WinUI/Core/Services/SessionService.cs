using POS.WinUI.Core.Constants;

namespace POS.WinUI.Core.Services;

public sealed class SessionService
{
    private readonly HashSet<string> _permissions = new(StringComparer.OrdinalIgnoreCase);

    public static SessionService? Current { get; private set; }

    public SessionService()
    {
        Current = this;
    }

    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public string? EmployeeId { get; private set; }
    public string? EmployeeName { get; private set; }
    public string? Role { get; private set; }
    public string? StoreId { get; private set; }
    public string? StoreName { get; private set; }
    public string? ShiftId { get; private set; }
    public string? ShiftName { get; private set; }
    public bool IsChainOwner { get; private set; }

    public bool IsLoggedIn => !string.IsNullOrEmpty(AccessToken);

    public bool IsManager => string.Equals(Role, AppRoles.StoreManager, StringComparison.OrdinalIgnoreCase)
                          || string.Equals(Role, AppRoles.Owner, StringComparison.OrdinalIgnoreCase);

    public bool IsOwner => IsChainOwner
                        || string.Equals(Role, AppRoles.Owner, StringComparison.OrdinalIgnoreCase);

    public bool IsStoreManager => string.Equals(Role, AppRoles.StoreManager, StringComparison.OrdinalIgnoreCase);

    public bool IsCashier => string.Equals(Role, AppRoles.Cashier, StringComparison.OrdinalIgnoreCase);

    public int RoleLevel => AppRoles.GetRoleLevel(Role);

    public IReadOnlyCollection<string> Permissions => _permissions;

    public string? EffectiveStoreId => StoreId;

    public event Action<string, string?>? StoreChanged;

    public void SetSession(
        string accessToken,
        string refreshToken,
        string employeeId,
        string employeeName,
        string role,
        string storeId,
        string? storeName = null,
        bool isChainOwner = false,
        IEnumerable<string>? permissions = null)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        EmployeeId = employeeId;
        EmployeeName = employeeName;
        Role = role;
        StoreId = storeId;
        IsChainOwner = isChainOwner;

        if (!string.IsNullOrWhiteSpace(storeName))
        {
            StoreName = storeName;
        }

        _permissions.Clear();
        if (permissions != null)
        {
            foreach (var p in permissions)
            {
                if (!string.IsNullOrWhiteSpace(p))
                {
                    _permissions.Add(p.Trim());
                }
            }
        }
    }

    public void SetStore(string storeId, string? storeName = null)
    {
        StoreId = storeId;
        if (!string.IsNullOrWhiteSpace(storeName))
        {
            StoreName = storeName;
        }
        StoreChanged?.Invoke(storeId, storeName);
    }

    public void SetShift(string shiftId, string? shiftName = null)
    {
        ShiftId = shiftId;
        ShiftName = shiftName;
    }

    public void RefreshAccessToken(string newAccessToken) => AccessToken = newAccessToken;

    public bool HasPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission)) return false;
        if (IsOwner) return true;
        return _permissions.Contains(permission);
    }

    public bool HasAnyPermission(params string[] permissions)
    {
        if (IsOwner) return true;
        if (permissions == null || permissions.Length == 0) return false;
        return permissions.Any(p => _permissions.Contains(p));
    }

    public bool IsInRole(string? roleList)
    {
        if (string.IsNullOrWhiteSpace(roleList) || string.IsNullOrWhiteSpace(Role))
            return false;

        var roles = roleList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return roles.Any(r => string.Equals(r, Role, StringComparison.OrdinalIgnoreCase)
                           || (string.Equals(r, AppRoles.Owner, StringComparison.OrdinalIgnoreCase) && IsChainOwner));
    }

    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
        EmployeeId = null;
        EmployeeName = null;
        Role = null;
        StoreId = null;
        StoreName = null;
        ShiftId = null;
        ShiftName = null;
        IsChainOwner = false;
        _permissions.Clear();
    }
}
