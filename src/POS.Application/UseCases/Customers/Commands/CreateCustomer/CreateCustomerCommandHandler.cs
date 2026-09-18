using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Customers;
using POS.Domain.Customers.Errors;

namespace POS.Application.UseCases.Customers.Commands.CreateCustomer;

public class CreateCustomerCommandHandler(
    ICustomerRepository customerRepository,
    IMemberTierRepository memberTierRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateCustomerCommand, CustomerDto>
{
    public async Task<Result<CustomerDto>> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var trimmedPhone = request.Phone.Trim();
        var trimmedBarcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();
        var trimmedEmail = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        if (!await customerRepository.IsPhoneUniqueAsync(trimmedPhone, cancellationToken: cancellationToken))
        {
            return CustomerErrors.DuplicatePhone;
        }

        if (trimmedBarcode is not null && !await customerRepository.IsBarcodeUniqueAsync(trimmedBarcode, cancellationToken: cancellationToken))
        {
            return CustomerErrors.DuplicateBarcode;
        }

        var defaultTier = await memberTierRepository.GetDefaultTierAsync(cancellationToken);
        if (defaultTier is null)
        {
            return CustomerErrors.MemberTierNotFound;
        }

        var customer = new Customer(
            name: request.Name.Trim(),
            phone: trimmedPhone,
            memberTierId: defaultTier.Id,
            email: trimmedEmail,
            dob: request.Dob,
            barcode: trimmedBarcode,
            isActive: true);

        var loyaltyAccount = new LoyaltyAccount(customer.Id, pointsBalance: 0);

        await customerRepository.AddAsync(customer, loyaltyAccount, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CustomerDto(
            Id: customer.Id,
            Name: customer.Name,
            Phone: customer.Phone,
            Email: customer.Email,
            Dob: customer.Dob,
            Barcode: customer.Barcode,
            MemberTierId: defaultTier.Id,
            MemberTierName: defaultTier.Name.ToString(),
            TotalSpending: customer.TotalSpending,
            PointsBalance: loyaltyAccount.PointsBalance,
            IsActive: customer.IsActive,
            CreatedAt: new DateTimeOffset(customer.CreatedAt, TimeSpan.Zero));
    }
}
