using MediatR;
using POS.Domain.Common;

namespace POS.Application.UseCases.Customers.Queries.GetPointTransactions;

public record GetPointTransactionsQuery(
    Guid CustomerId,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Type = null,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<Result<PagedPointTransactionList>>;
