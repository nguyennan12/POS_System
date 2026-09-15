namespace POS.Application.Abstractions.Auth;

public interface IRequirePermission
{
    string RequiredPermission { get; }
}