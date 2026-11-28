using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.StoreLayouts.Queries.GetActiveStoreLayout;

public sealed record GetActiveStoreLayoutQuery(
    Guid? StoreId = null) : IQuery<StoreLayoutDetailDto?>, IRequirePermission
{
    public string RequiredPermission => "store_layouts:read";
}
