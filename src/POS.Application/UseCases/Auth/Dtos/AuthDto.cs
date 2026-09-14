using POS.Domain.Employees;

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
)
{
  public static AuthDto ToDto(
      Employee employee,
      string accessToken,
      string refreshToken,
      DateTimeOffset expiresAt,
      IReadOnlyList<string> permissions)
  {
    return new AuthDto(
        accessToken,
        refreshToken,
        expiresAt,
        new CurrentUserDto(
            employee.Id,
            employee.Name,
            employee.Username,
            employee.RoleId,
            employee.Role.Name,
            employee.StoreId,
            employee.Store?.Name,
            employee.IsChainOwner,
            permissions));
  }
}
