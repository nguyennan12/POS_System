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
      dto.User.ToResponse()
    );
  }

  public static CurrentUserResponse ToResponse(this CurrentUserDto dto)
  {
    return new CurrentUserResponse(
      dto.Id,
      dto.Name,
      dto.Username,
      dto.RoleId,
      dto.RoleName,
      dto.StoreId,
      dto.StoreName,
      dto.IsChainOwner,
      dto.Permissions
    );
  }
}