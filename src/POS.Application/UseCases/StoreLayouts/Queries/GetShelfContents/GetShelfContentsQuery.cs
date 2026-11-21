using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.StoreLayouts.Queries.GetShelfContents;

public sealed record GetShelfContentsQuery(
    Guid ShelfId,
    Guid? StoreId = null) : IQuery<ShelfContentsDto>, IRequirePermission
{
    public string RequiredPermission => "product_placements:read";
}
