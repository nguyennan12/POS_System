using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Auth;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Customers.Queries.GetCustomers;
using static POS.Application.UseCases.AI.Tools.Common.AiToolArgumentReader;

namespace POS.Application.UseCases.AI.Tools.Customers;

public sealed class SearchCustomersToolHandler(ISender sender, IStoreAccessPolicy storeAccess) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties = ["phone", "name", "barcode", "limit"];

    public string Name => AiToolNames.SearchCustomers;

    public async Task<AiToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !TryReadOptionalString(arguments, "phone", 30, out var phone, out error) ||
            !TryReadOptionalString(arguments, "name", 100, out var name, out error) ||
            !TryReadOptionalString(arguments, "barcode", 100, out var barcode, out error) ||
            !TryReadOptionalInt(arguments, "limit", 10, 1, 20, out var limit, out error))
            return error!;

        if (phone is null && name is null && barcode is null)
            return AiToolErrors.InvalidArgument("Cần cung cấp số điện thoại, tên hoặc mã vạch của khách hàng.");

        var store = storeAccess.EnforceStoreAccess();
        if (store.IsFailure)
            return AiToolErrors.FromDomainError(store.Error);

        var result = await sender.Send(new GetCustomersQuery(phone, name, barcode, PageSize: limit)
        {
            ScopedStoreId = store.Value
        }, cancellationToken);
        if (result.IsFailure)
            return AiToolErrors.FromDomainError(result.Error);

        var customers = result.Value!.Items.Select(customer => new
        {
            CustomerId = customer.Id,
            FullName = customer.Name,
            customer.Phone,
            customer.MemberTierName,
            customer.PointsBalance
        }).ToArray();
        return AiToolExecutionResult.Ok(customers, customers.Length);
    }
}
