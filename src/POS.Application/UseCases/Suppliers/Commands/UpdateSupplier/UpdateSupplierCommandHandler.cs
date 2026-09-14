using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.Common;
using POS.Domain.Common;

namespace POS.Application.UseCases.Suppliers.Commands.UpdateSupplier;

public class UpdateSupplierCommandHandler : ICommandHandler<UpdateSupplierCommand, SupplierDto>
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSupplierCommandHandler(
        ISupplierRepository supplierRepository,
        IUnitOfWork unitOfWork)
    {
        _supplierRepository = supplierRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SupplierDto>> Handle(
        UpdateSupplierCommand command,
        CancellationToken cancellationToken)
    {
        var supplier = await _supplierRepository.GetByIdAsync(command.SupplierId, cancellationToken);

        if (supplier is null)
            return CommonErrors.NotFound("Supplier");

        supplier.Update(
            command.Name,
            command.TaxCode,
            command.ContactName,
            command.Phone,
            command.Email,
            command.Address,
            command.CreditTerms,
            command.IsActive);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return supplier.ToDto();
    }
}
