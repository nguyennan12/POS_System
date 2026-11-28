using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.StoreLayouts.Queries.GetStoreLayoutById;

public sealed record GetStoreLayoutByIdQuery(
    Guid LayoutId) : IQuery<StoreLayoutDetailDto>, IRequirePermission
{
    public string RequiredPermission => "store_layouts:read";
}
