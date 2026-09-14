using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Suppliers.Queries.GetSupplierById;

public record GetSupplierByIdQuery(Guid SupplierId) : IQuery<SupplierDto>;
