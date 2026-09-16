using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Suppliers.Commands.DeleteSupplier;

/// <summary>Soft-delete: marks supplier as IsActive = false.</summary>
public record DeleteSupplierCommand(Guid SupplierId) : ICommand, IRequirePermission
{
	public string RequiredPermission => "suppliers:delete";
}
