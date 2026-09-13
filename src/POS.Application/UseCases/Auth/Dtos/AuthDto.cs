namespace POS.Application.UseCases.Auth.Dtos;

public record CurrentUserDto(
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

public record AuthDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    CurrentUserDto User
);