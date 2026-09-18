using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Customers.Queries.GetCustomers;

public class GetCustomersQueryHandler(ICustomerRepository customerRepository)
    : IQueryHandler<GetCustomersQuery, PagedCustomerList>
{
    public async Task<Result<PagedCustomerList>> Handle(
        GetCustomersQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await customerRepository.GetPagedAsync(
            request.Phone,
            request.Name,
            request.Barcode,
            request.MemberTierId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtoList = items.Select(item => new CustomerDto(
            Id: item.Customer.Id,
            Name: item.Customer.Name,
            Phone: item.Customer.Phone,
            Email: item.Customer.Email,
            Dob: item.Customer.Dob,
            Barcode: item.Customer.Barcode,
            MemberTierId: item.Customer.MemberTierId,
            MemberTierName: item.Customer.MemberTier?.Name.ToString() ?? "Normal",
            TotalSpending: item.Customer.TotalSpending,
            PointsBalance: item.PointsBalance,
            IsActive: item.Customer.IsActive,
            CreatedAt: new DateTimeOffset(item.Customer.CreatedAt, TimeSpan.Zero)
        )).ToList();

        return new PagedCustomerList(
            dtoList,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
