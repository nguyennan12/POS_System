using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.Common;
using POS.Domain.Common;

namespace POS.Application.UseCases.Suppliers.Queries.GetSupplierById;

public class GetSupplierByIdQueryHandler : IQueryHandler<GetSupplierByIdQuery, SupplierDto>
{
    private readonly ISupplierRepository _supplierRepository;

    public GetSupplierByIdQueryHandler(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<Result<SupplierDto>> Handle(
        GetSupplierByIdQuery query,
        CancellationToken cancellationToken)
    {
        var supplier = await _supplierRepository.GetByIdAsync(query.SupplierId, cancellationToken);

        if (supplier is null)
            return CommonErrors.NotFound("Supplier");

        return supplier.ToDto();
    }
}
