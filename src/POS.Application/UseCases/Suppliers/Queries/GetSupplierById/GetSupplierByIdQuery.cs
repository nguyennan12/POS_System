using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Suppliers.Queries.GetSupplierById;

public record GetSupplierByIdQuery(Guid SupplierId) : IQuery<SupplierDto>, IRequirePermission
{
	public string RequiredPermission => "suppliers:read";
}
