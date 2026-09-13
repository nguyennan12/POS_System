using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Inventory.Suppliers;

namespace POS.Application.UseCases.Suppliers.Commands.CreateSupplier;

public class CreateSupplierCommandHandler : ICommandHandler<CreateSupplierCommand, SupplierDto>
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSupplierCommandHandler(
        ISupplierRepository supplierRepository,
        IUnitOfWork unitOfWork)
    {
        _supplierRepository = supplierRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SupplierDto>> Handle(
        CreateSupplierCommand command,
        CancellationToken cancellationToken)
    {
        var supplier = new Supplier(
            command.Name,
            command.TaxCode,
            command.ContactName,
            command.Phone,
            command.Email,
            command.Address,
            command.CreditTerms);

        await _supplierRepository.AddAsync(supplier, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return supplier.ToDto();
    }
}
