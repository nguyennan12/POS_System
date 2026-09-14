using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using POS.Application.Abstractions.Auth;

namespace POS.Infrastructure.Auth;

public class PinLookupHasher : IPinLookupHasher
{
  private readonly string _secret;

  public PinLookupHasher(IConfiguration configuration)
  {
    _secret = configuration["Auth:PinLookupSecret"]
        ?? throw new InvalidOperationException("Auth:PinLookupSecret is not configured.");
  }

  public string ComputeHash(string pin)
  {
    var keyBytes = Encoding.UTF8.GetBytes(_secret);
    var pinBytes = Encoding.UTF8.GetBytes(pin);

    using var hmac = new HMACSHA256(keyBytes);
    var hashBytes = hmac.ComputeHash(pinBytes);

    return Convert.ToHexString(hashBytes).ToLowerInvariant();
  }
}