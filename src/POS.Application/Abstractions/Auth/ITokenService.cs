using POS.Domain.Employees;

namespace POS.Application.Abstractions.Auth;

public interface ITokenService
{
  AuthTokenResult CreateAccessToken(TokenSubject subject);
  RefreshTokenResult CreateRefreshToken();
}


public record AuthTokenResult(
    string AccessToken,
    DateTimeOffset ExpiresAt
);

public record RefreshTokenResult(
    string RefreshTokenoken,
    string RefreshTokenHash,
    DateTime ExpiresAt
);

public record TokenSubject(
    Guid SubjectId,
    string SubjectType,
    Guid? RoleId,
    Guid? StoreId,
    bool IsChainOwner,
    IReadOnlyList<string> Permissions
);