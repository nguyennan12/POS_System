namespace POS.Domain.Auditing;

public static class AuthenticationAuditActions
{
    public const string Login = "Login";
    public const string LoginFailed = "LoginFailed";
    public const string Logout = "Logout";
    public const string PasswordChanged = "PasswordChanged";
    public const string PinChanged = "PinChanged";
}
