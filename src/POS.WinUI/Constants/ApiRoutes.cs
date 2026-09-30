namespace POS.WinUI.Constants;

/// <summary>
/// Tập trung tất cả URL endpoint của POS API.
/// Khi base path thay đổi chỉ cần sửa tại đây.
/// </summary>
public static class ApiRoutes
{
    public const string Health = "health";

    public static class Auth
    {
        public const string Login   = "api/v1/auth/employee/login";
        public const string PinLogin = "api/v1/auth/employee/pin";
        public const string Refresh  = "api/v1/auth/refresh";
        public const string Logout   = "api/v1/auth/logout";
    }

    public static class Stores
    {
        public const string Public  = "api/v1/stores/public";
        public const string All     = "api/v1/stores";
    }

    public static class Shifts
    {
        public const string Open = "api/v1/shifts/open";
        public const string Current = "api/v1/shifts/current";
        public static string GetById(Guid id) => $"api/v1/shifts/{id}";
        public static string Close(Guid id) => $"api/v1/shifts/{id}/close";
    }
}
