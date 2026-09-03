namespace POS.Contracts.V1.Auth;

public record CurrentUserResponse(
    Guid Id,
    string Name,
    string Username,
    Guid RoleId,
    string RoleName,
    Guid? StoreId,
    string? StoreName,
    bool IsChainOwner,
    IReadOnlyList<string> Permissions
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    CurrentUserResponse User
);
