using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Suppliers.Queries.GetSupplierPayments;

public record GetSupplierPaymentsQuery(Guid SupplierId) : IQuery<List<SupplierPaymentDto>>;
