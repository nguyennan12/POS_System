namespace POS.WinUI.Constants;

public static class AppRoles
{
    public const string Owner = "Owner";
    public const string StoreManager = "StoreManager";
    public const string Cashier = "Cashier";

    public static int GetRoleLevel(string? role) => role switch
    {
        Owner => 3,
        StoreManager => 2,
        Cashier => 1,
        _ => 0
    };
}
