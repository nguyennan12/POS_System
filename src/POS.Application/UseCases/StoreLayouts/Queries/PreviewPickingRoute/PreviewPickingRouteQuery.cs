using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.StoreLayouts.Queries.PreviewPickingRoute;

public sealed record PreviewPickingRouteQuery(
    IReadOnlyList<PickingRouteItemDto> Items,
    Guid? StoreId = null,
    LayoutPointDto? StartCell = null,
    Guid? LayoutId = null) : IQuery<PickingRoutePreviewDto>, IRequirePermission
{
    public string RequiredPermission => "store_layouts:read";
}
