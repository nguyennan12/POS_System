namespace POS.Application.Abstractions.Auth;

public interface ICurrentUser
{
    Guid? EmployeeId { get; }
    Guid? RoleId { get; }
    Guid? StoreId { get; }
    bool IsChainOwner { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
