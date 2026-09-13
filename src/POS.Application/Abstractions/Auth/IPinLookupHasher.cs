namespace POS.Application.Abstractions.Auth;

public interface IPinLookupHasher
{
  string ComputeHash(string pin);
}