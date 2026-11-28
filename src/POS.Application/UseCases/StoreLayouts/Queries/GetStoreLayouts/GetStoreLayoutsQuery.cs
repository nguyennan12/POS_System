using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Domain.StoreLayouts.Enums;

namespace POS.Application.UseCases.StoreLayouts.Queries.GetStoreLayouts;

public sealed record GetStoreLayoutsQuery(
    Guid? StoreId = null,
    StoreLayoutStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagedStoreLayoutsDto>, IRequirePermission
{
    public string RequiredPermission => "store_layouts:read";
}
