using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Auth;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Customers.Queries.GetCustomerById;
using static POS.Application.UseCases.AI.Tools.Common.AiToolArgumentReader;

namespace POS.Application.UseCases.AI.Tools.Customers;

public sealed class GetCustomerDetailToolHandler(ISender sender, IStoreAccessPolicy storeAccess) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties = ["customerId"];

    public string Name => AiToolNames.GetCustomerDetail;

    public async Task<AiToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !TryReadRequiredGuid(arguments, "customerId", out var customerId, out error))
            return error!;

        var store = storeAccess.EnforceStoreAccess();
        if (store.IsFailure)
            return AiToolErrors.FromDomainError(store.Error);

        var result = await sender.Send(new GetCustomerByIdQuery(customerId)
        {
            ScopedStoreId = store.Value
        }, cancellationToken);
        if (result.IsFailure)
            return AiToolErrors.FromDomainError(result.Error);

        var customer = result.Value!;
        return AiToolExecutionResult.Ok(new
        {
            CustomerId = customer.Id,
            FullName = customer.Name,
            customer.Phone,
            customer.MemberTierName,
            customer.PointsBalance,
            customer.IsActive
        }, 1);
    }
}
