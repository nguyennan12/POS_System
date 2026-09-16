using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Customers.Errors;

namespace POS.Application.UseCases.Customers.Commands.UpdateCustomer;

public class UpdateCustomerCommandHandler(
    ICustomerRepository customerRepository,
    IMemberTierRepository memberTierRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateCustomerCommand, CustomerDto>
{
    public async Task<Result<CustomerDto>> Handle(
        UpdateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetEntityByIdAsync(request.Id, cancellationToken);
        if (customer is null)
        {
            return CustomerErrors.NotFound;
        }

        var trimmedPhone = request.Phone.Trim();
        var trimmedBarcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();
        var trimmedEmail = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        if (!await customerRepository.IsPhoneUniqueAsync(trimmedPhone, excludeCustomerId: customer.Id, cancellationToken: cancellationToken))
        {
            return CustomerErrors.DuplicatePhone;
        }

        if (trimmedBarcode is not null && !await customerRepository.IsBarcodeUniqueAsync(trimmedBarcode, excludeCustomerId: customer.Id, cancellationToken: cancellationToken))
        {
            return CustomerErrors.DuplicateBarcode;
        }

        customer.Update(
            name: request.Name.Trim(),
            phone: trimmedPhone,
            email: trimmedEmail,
            dob: request.Dob,
            barcode: trimmedBarcode,
            isActive: request.IsActive);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var details = await customerRepository.GetByIdAsync(customer.Id, cancellationToken);
        var tier = details?.Customer.MemberTier ?? await memberTierRepository.GetByIdAsync(customer.MemberTierId, cancellationToken);

        return new CustomerDto(
            Id: customer.Id,
            Name: customer.Name,
            Phone: customer.Phone,
            Email: customer.Email,
            Dob: customer.Dob,
            Barcode: customer.Barcode,
            MemberTierId: customer.MemberTierId,
            MemberTierName: tier?.Name.ToString() ?? "Normal",
            TotalSpending: customer.TotalSpending,
            PointsBalance: details?.PointsBalance ?? 0,
            IsActive: customer.IsActive,
            CreatedAt: new DateTimeOffset(customer.CreatedAt, TimeSpan.Zero));
    }
}
