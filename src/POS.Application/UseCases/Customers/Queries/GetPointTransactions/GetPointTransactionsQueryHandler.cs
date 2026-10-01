using MediatR;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Customers.Enums;
using POS.Domain.Customers.Errors;

namespace POS.Application.UseCases.Customers.Queries.GetPointTransactions;

public class GetPointTransactionsQueryHandler(ICustomerRepository customerRepository)
    : IRequestHandler<GetPointTransactionsQuery, Result<PagedPointTransactionList>>
{
    public async Task<Result<PagedPointTransactionList>> Handle(GetPointTransactionsQuery request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetEntityByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return CustomerErrors.NotFound;
        }

        PointTransactionType? transactionType = null;
        if (!string.IsNullOrWhiteSpace(request.Type) && Enum.TryParse<PointTransactionType>(request.Type, true, out var parsedType))
        {
            transactionType = parsedType;
        }

        var (items, totalCount) = await customerRepository.GetPointTransactionsPagedAsync(
            request.CustomerId,
            request.From,
            request.To,
            transactionType,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(t => new PointTransactionDto(
            t.Id,
            t.CustomerId,
            t.Points,
            t.Type.ToString(),
            t.OrderId,
            t.Note,
            t.CreatedAt
        )).ToList();

        return new PagedPointTransactionList(
            dtos,
            totalCount,
            request.PageNumber,
            request.PageSize
        );
    }
}
