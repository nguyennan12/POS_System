namespace POS.Application.Abstractions.Auth;

public interface IPasswordHasher
{
  string Hash(string value);

  bool Verify(string value, string hash);
}