using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using POS.Application.Abstractions.Auth;

namespace POS.Infrastructure.Auth;

public class TokenService : ITokenService
{
  private readonly JwtOptions _options;
  public TokenService(IOptions<JwtOptions> options)
  {
    _options = options.Value;
  }
  public AuthTokenResult CreateAccessToken(TokenSubject subject)
  {
    var expiresAt = DateTimeOffset.UtcNow.AddHours(_options.AccessTokenExpiryHours);

    var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject.SubjectId.ToString()),
            new("subject_type", subject.SubjectType),
            new("role_id", subject.RoleId?.ToString() ?? string.Empty),
            new("store_id", subject.StoreId?.ToString() ?? string.Empty),
            new("is_chain_owner", subject.IsChainOwner.ToString())
        };

    foreach (var permission in subject.Permissions)
      claims.Add(new Claim("permission", permission));

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
          issuer: _options.Issuer,
          audience: _options.Audience,
          claims: claims,
          expires: expiresAt.UtcDateTime,
          signingCredentials: credentials);

    var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

    return new AuthTokenResult(accessToken, expiresAt);
  }

  public RefreshTokenResult CreateRefreshToken()
  {
    var refreshToken = WebEncoders.Base64UrlEncode(
      RandomNumberGenerator.GetBytes(64));

    var refreshTokenHash = HashRefreshToken(refreshToken);

    var expiresAt = DateTime.UtcNow
        .AddDays(_options.RefreshTokenExpiryDays);

    return new RefreshTokenResult(
        refreshToken,
        refreshTokenHash,
        expiresAt);
  }

  private string HashRefreshToken(string refreshToken)
  {
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
    return Convert.ToHexString(bytes).ToLowerInvariant();
  }
}