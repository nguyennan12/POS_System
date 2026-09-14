using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.Common;
using POS.Domain.Common;

namespace POS.Application.UseCases.Suppliers.Commands.DeleteSupplier;

public class DeleteSupplierCommandHandler : ICommandHandler<DeleteSupplierCommand>
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSupplierCommandHandler(
        ISupplierRepository supplierRepository,
        IUnitOfWork unitOfWork)
    {
        _supplierRepository = supplierRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        DeleteSupplierCommand command,
        CancellationToken cancellationToken)
    {
        var supplier = await _supplierRepository.GetByIdAsync(command.SupplierId, cancellationToken);

        if (supplier is null)
            return CommonErrors.NotFound("Supplier");

        supplier.Deactivate();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
