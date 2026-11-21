using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.StoreLayouts.Queries.GetPlacementHistory;

public sealed record GetPlacementHistoryQuery(
    Guid SkuId,
    Guid? StoreId = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagedPlacementHistoryDto>, IRequirePermission
{
    public string RequiredPermission => "product_placements:read";
}
