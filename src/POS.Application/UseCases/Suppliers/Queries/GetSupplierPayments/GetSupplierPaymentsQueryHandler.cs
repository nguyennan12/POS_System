using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.Common;
using POS.Domain.Common;

namespace POS.Application.UseCases.Suppliers.Queries.GetSupplierPayments;

public class GetSupplierPaymentsQueryHandler
    : IQueryHandler<GetSupplierPaymentsQuery, List<SupplierPaymentDto>>
{
    private readonly ISupplierRepository _supplierRepository;

    public GetSupplierPaymentsQueryHandler(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<Result<List<SupplierPaymentDto>>> Handle(
        GetSupplierPaymentsQuery query,
        CancellationToken cancellationToken)
    {
        var supplier = await _supplierRepository.GetByIdAsync(query.SupplierId, cancellationToken);

        if (supplier is null)
            return CommonErrors.NotFound("Supplier");

        var payments = await _supplierRepository.GetPaymentsBySupplierAsync(
            query.SupplierId, cancellationToken);

        return payments.Select(p => p.ToDto()).ToList();
    }
}
