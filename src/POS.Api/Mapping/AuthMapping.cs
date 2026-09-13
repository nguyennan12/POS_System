using POS.Application.UseCases.Auth;
using POS.Application.UseCases.Auth.Dtos;
using POS.Contracts.V1.Auth;

namespace POS.Api.Mapping;

public static class AuthMapping
{
  public static AuthResponse ToResponse(this AuthDto dto)
  {
    return new AuthResponse(
      dto.AccessToken,
      dto.RefreshToken,
      dto.ExpiresAt,
      new CurrentUserResponse(
        dto.User.Id,
        dto.User.Name,
        dto.User.Username,
        dto.User.RoleId,
        dto.User.RoleName,
        dto.User.StoreId,
        dto.User.StoreName,
        dto.User.IsChainOwner,
        dto.User.Permissions)
    );
  }
}