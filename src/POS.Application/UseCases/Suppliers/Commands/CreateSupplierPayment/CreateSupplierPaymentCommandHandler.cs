using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.Common;
using POS.Domain.Common;
using POS.Domain.Inventory.Enums;
using POS.Domain.Inventory.Suppliers;

namespace POS.Application.UseCases.Suppliers.Commands.CreateSupplierPayment;

public class CreateSupplierPaymentCommandHandler
    : ICommandHandler<CreateSupplierPaymentCommand, SupplierPaymentDto>
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateSupplierPaymentCommandHandler(
        ISupplierRepository supplierRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _supplierRepository = supplierRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<SupplierPaymentDto>> Handle(
        CreateSupplierPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var supplier = await _supplierRepository.GetByIdAsync(command.SupplierId, cancellationToken);

        if (supplier is null)
            return CommonErrors.NotFound("Supplier");

        if (!supplier.IsActive)
            return CommonErrors.Invalid("Supplier.IsActive");

        if (supplier.Address is null)
            return CommonErrors.Invalid("Supplier.Address");

        if (supplier.Email is null && supplier.Phone is null)
            return CommonErrors.Invalid("Supplier.EmailOrPhone");

        if (command.VoucherId is null)
            return CommonErrors.Invalid("VoucherId");

        if (!Enum.TryParse<SupplierPaymentMethod>(command.Method, ignoreCase: true, out var method))
            return CommonErrors.Invalid("Method");

        if (_currentUser.EmployeeId is null)
            return CommonErrors.Invalid("CurrentUser.EmployeeId");

        var employeeId = _currentUser.EmployeeId.Value;

        var payment = new SupplierPayment(
            command.SupplierId,
            command.Amount,
            method,
            employeeId,
            command.VoucherId,
            command.Note);

        await _supplierRepository.AddPaymentAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return payment.ToDto();
    }
}
