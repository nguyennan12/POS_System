using System.Security.Claims;
using POS.Application.Abstractions.Auth;

namespace POS.Api.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? EmployeeId
    {
        get
        {
            var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                               _httpContextAccessor.HttpContext?.User?.FindFirstValue("employee_id") ??
                               _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
                               
            if (Guid.TryParse(userIdString, out var employeeId))
            {
                return employeeId;
            }

            return null;
        }
    }

    public Guid? StoreId
    {
        get
        {
            var storeIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue("store_id");
            if (Guid.TryParse(storeIdString, out var storeId))
            {
                return storeId;
            }
            return null;
        }
    }

    public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role) ?? 
                           _httpContextAccessor.HttpContext?.User?.FindFirstValue("role");

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
