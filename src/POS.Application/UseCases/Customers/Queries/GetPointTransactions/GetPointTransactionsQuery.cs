using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Customers.Queries.GetPointTransactions;

///  
/// Requests a page of customer point transactions with optional date and type filters.
/// </summary>
public record GetPointTransactionsQuery(
    Guid CustomerId,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Type = null,
    int PageNumber = 1,
    int PageSize = 20
) : IQuery<PagedPointTransactionList>, IRequirePermission
{
    public string RequiredPermission => "customers:read";
}
