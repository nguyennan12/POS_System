namespace POS.Infrastructure.Auth;

public class JwtOptions
{
  public string Secret { get; init; } = default!;
  public string Issuer { get; init; } = default!;
  public string Audience { get; init; } = default!;
  public int AccessTokenExpiryHours { get; init; }
  public int RefreshTokenExpiryDays { get; init; }
}