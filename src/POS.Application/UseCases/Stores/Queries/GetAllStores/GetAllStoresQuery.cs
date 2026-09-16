using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Stores.Queries.GetAllStores;

public record GetAllStoresQuery : IQuery<List<StoreDto>>, IRequirePermission
{
	public string RequiredPermission => "stores:read";
}
public record StoreDto(Guid Id, string Name, string? Address, string? Phone, bool IsActive);
