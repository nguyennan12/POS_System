using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Auth;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Shifts.Queries.GetCurrentShift;
using POS.Domain.Employees;
using static POS.Application.UseCases.AI.Tools.Common.AiToolArgumentReader;

namespace POS.Application.UseCases.AI.Tools.Shifts;

public sealed class GetCurrentShiftToolHandler(ISender sender, IStoreAccessPolicy storeAccess) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties = ["registerId"];

    public string Name => AiToolNames.GetCurrentShift;

    public async Task<AiToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !TryReadOptionalGuid(arguments, "registerId", out var registerId, out error))
            return error!;

        var store = storeAccess.EnforceStoreAccess();
        if (store.IsFailure)
            return AiToolErrors.FromDomainError(store.Error);

        var result = await sender.Send(new GetCurrentShiftQuery(store.Value, registerId)
        {
            OwnShiftOnly = true
        }, cancellationToken);
        if (result.IsSuccess)
            return AiToolExecutionResult.Ok(new { IsOpen = true, Shift = result.Value }, 1);

        return result.Error == ShiftErrors.NotFound
            ? AiToolExecutionResult.Ok(new { IsOpen = false }, 0)
            : AiToolErrors.FromDomainError(result.Error);
    }
}
