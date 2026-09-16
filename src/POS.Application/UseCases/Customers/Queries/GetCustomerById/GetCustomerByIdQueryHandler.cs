using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Customers.Errors;

namespace POS.Application.UseCases.Customers.Queries.GetCustomerById;

public class GetCustomerByIdQueryHandler(ICustomerRepository customerRepository)
    : IQueryHandler<GetCustomerByIdQuery, CustomerDto>
{
    public async Task<Result<CustomerDto>> Handle(
        GetCustomerByIdQuery request,
        CancellationToken cancellationToken)
    {
        var customerWithPoints = await customerRepository.GetByIdAsync(request.Id, cancellationToken);
        if (customerWithPoints is null)
        {
            return CustomerErrors.NotFound;
        }

        var c = customerWithPoints.Customer;
        return new CustomerDto(
            Id: c.Id,
            Name: c.Name,
            Phone: c.Phone,
            Email: c.Email,
            Dob: c.Dob,
            Barcode: c.Barcode,
            MemberTierId: c.MemberTierId,
            MemberTierName: c.MemberTier?.Name.ToString() ?? "Normal",
            TotalSpending: c.TotalSpending,
            PointsBalance: customerWithPoints.PointsBalance,
            IsActive: c.IsActive,
            CreatedAt: new DateTimeOffset(c.CreatedAt, TimeSpan.Zero));
    }
}
