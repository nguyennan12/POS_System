using System.Security.Claims;
using POS.Application.Abstractions.Auth;

namespace POS.Api.Auth;

// Only adapts the authenticated HTTP principal to the existing application interface.
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    // TokenService emits sub; JwtBearer maps it to NameIdentifier by default.
    // Retain employee_id as a fallback for existing principals using the legacy claim.
    public Guid? EmployeeId => ReadGuid("sub") ?? ReadGuid(ClaimTypes.NameIdentifier) ?? ReadGuid("employee_id");
    public Guid? StoreId => ReadGuid("store_id");
    public string? Role => User?.FindFirstValue(ClaimTypes.Role) ?? User?.FindFirstValue("role");

    private Guid? ReadGuid(string claim) =>
        IsAuthenticated && Guid.TryParse(User?.FindFirstValue(claim), out var id) && id != Guid.Empty ? id : null;
}
