using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.StoreLayouts.Queries.GetProductLocations;

public sealed record GetProductLocationsQuery(
    Guid SkuId,
    Guid? StoreId = null) : IQuery<ProductLocationsDto>, IRequirePermission
{
    public string RequiredPermission => "product_placements:read";
}
