using System.Security.Claims;
using POS.Application.Abstractions.Auth;

namespace POS.Api.Auth;

// Only adapts the authenticated HTTP principal to the existing application interface.
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    public Guid? EmployeeId => ReadGuid("employee_id");
    public Guid? StoreId => ReadGuid("store_id");
    public string? Role => User?.FindFirstValue(ClaimTypes.Role) ?? User?.FindFirstValue("role");

    private Guid? ReadGuid(string claim) =>
        IsAuthenticated && Guid.TryParse(User?.FindFirstValue(claim), out var id) && id != Guid.Empty ? id : null;
}
