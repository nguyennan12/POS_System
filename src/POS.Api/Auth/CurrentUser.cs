using System.Security.Claims;
using POS.Application.Abstractions.Auth;

namespace POS.Api.Auth;

// Only adapts the authenticated HTTP principal to the existing application interface.
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    
    public Guid? EmployeeId => ReadGuid("sub") ?? ReadGuid(ClaimTypes.NameIdentifier) ?? ReadGuid("employee_id");
    public Guid? RoleId => ReadGuid("role_id");
    public Guid? StoreId
    {
        get
        {
            if (IsChainOwner && accessor.HttpContext != null)
            {
                if (accessor.HttpContext.Request.Headers.TryGetValue("X-Store-Id", out var headerVal) &&
                    Guid.TryParse(headerVal, out var headerStoreId) && headerStoreId != Guid.Empty)
                {
                    return headerStoreId;
                }

                if (accessor.HttpContext.Request.Query.TryGetValue("storeId", out var queryVal) &&
                    Guid.TryParse(queryVal, out var queryStoreId) && queryStoreId != Guid.Empty)
                {
                    return queryStoreId;
                }
            }

            return ReadGuid("store_id");
        }
    }
    public bool IsChainOwner => IsAuthenticated &&
        bool.TryParse(User?.FindFirstValue("is_chain_owner"), out var isChainOwner) && isChainOwner;
    public string? Role => User?.FindFirstValue(ClaimTypes.Role) ?? User?.FindFirstValue("role");

    private Guid? ReadGuid(string claim) =>
        IsAuthenticated && Guid.TryParse(User?.FindFirstValue(claim), out var id) && id != Guid.Empty ? id : null;
}
