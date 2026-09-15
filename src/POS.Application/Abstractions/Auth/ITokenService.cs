namespace POS.Application.Abstractions.Auth;

public interface ITokenService
{
  AuthTokenResult CreateAccessToken(TokenSubject subject);
  RefreshTokenResult CreateRefreshToken();
  string HashRefreshToken(string refreshToken);
}


public record AuthTokenResult(
    string AccessToken,
    DateTimeOffset ExpiresAt
);

public record RefreshTokenResult(
    string RefreshToken,
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
