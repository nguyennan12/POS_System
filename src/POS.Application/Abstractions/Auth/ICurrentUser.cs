namespace POS.Application.Abstractions.Auth;

public interface ICurrentUser
{
    Guid? EmployeeId { get; }
    Guid? StoreId { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
