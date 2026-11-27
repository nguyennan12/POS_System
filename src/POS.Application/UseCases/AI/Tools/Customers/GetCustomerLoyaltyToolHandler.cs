using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Auth;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Customers.Queries.GetLoyaltyAccount;
using static POS.Application.UseCases.AI.Tools.Common.AiToolArgumentReader;

namespace POS.Application.UseCases.AI.Tools.Customers;

public sealed class GetCustomerLoyaltyToolHandler(ISender sender, IStoreAccessPolicy storeAccess) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties = ["customerId"];

    public string Name => AiToolNames.GetCustomerLoyalty;

    public async Task<AiToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !TryReadRequiredGuid(arguments, "customerId", out var customerId, out error))
            return error!;

        var store = storeAccess.EnforceStoreAccess();
        if (store.IsFailure)
            return AiToolErrors.FromDomainError(store.Error);

        var result = await sender.Send(new GetLoyaltyAccountQuery(customerId)
        {
            ScopedStoreId = store.Value
        }, cancellationToken);
        if (result.IsFailure)
            return AiToolErrors.FromDomainError(result.Error);

        var account = result.Value!;
        return AiToolExecutionResult.Ok(new
        {
            account.CustomerId,
            account.PointsBalance,
            account.TierName,
            account.PointRate,
            account.DiscountRate
        }, 1);
    }
}
