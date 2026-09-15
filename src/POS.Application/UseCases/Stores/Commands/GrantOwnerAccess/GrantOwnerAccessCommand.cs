using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Stores.Queries.GetStoreDetail;

namespace POS.Application.UseCases.Stores.Commands.GrantOwnerAccess;

public record GrantOwnerAccessCommand(Guid StoreId, Guid EmployeeId) : ICommand<StoreDetailDto>, IRequirePermission
{
	public string RequiredPermission => "stores:update";
}
