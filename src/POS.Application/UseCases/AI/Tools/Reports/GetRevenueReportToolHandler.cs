using System.Text.Json;
using MediatR;
using POS.Application.Abstractions.AI;
using POS.Application.Abstractions.Auth;
using POS.Application.UseCases.AI.Tools.Common;
using POS.Application.UseCases.Reports.Queries.GetRevenueReport;

namespace POS.Application.UseCases.AI.Tools.Reports;

public sealed record RevenueByPeriodToolDto(
    string Period,
    decimal Revenue,
    decimal Discount,
    decimal Tax,
    decimal NetRevenue,
    int OrderCount);

public sealed record RevenueReportToolDto(
    decimal TotalRevenue,
    decimal TotalDiscount,
    decimal TotalTax,
    decimal NetRevenue,
    int TotalOrders,
    IReadOnlyList<RevenueByPeriodToolDto> Periods);

public sealed class GetRevenueReportToolHandler(
    ISender sender,
    IStoreAccessPolicy storeAccessPolicy) : IAiToolHandler
{
    private static readonly HashSet<string> AllowedProperties =
        new(StringComparer.Ordinal) { "from", "to" };

    public string Name => AiToolNames.GetRevenueReport;

    public async Task<AiToolExecutionResult> ExecuteAsync(
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        if (!AiToolArgumentReader.EnsureAllowedProperties(arguments, AllowedProperties, out var error) ||
            !AiToolArgumentReader.TryReadRequiredDateRangeUtc(arguments, "from", "to", out var fromUtc, out var toUtc, out error))
        {
            return error!;
        }

        var storeContext = storeAccessPolicy.EnforceStoreAccess();
        if (storeContext.IsFailure)
        {
            return AiToolErrors.FromDomainError(storeContext.Error);
        }

        var result = await sender.Send(
            new GetRevenueReportQuery(
                From: fromUtc,
                To: toUtc,
                StoreId: storeContext.Value,
                EmployeeId: null),
            cancellationToken);

        if (result.IsFailure)
        {
            return AiToolErrors.FromDomainError(result.Error);
        }

        var report = result.Value;
        var safeReport = new RevenueReportToolDto(
            TotalRevenue: report.TotalRevenue,
            TotalDiscount: report.TotalDiscount,
            TotalTax: report.TotalTax,
            NetRevenue: report.NetRevenue,
            TotalOrders: report.TotalOrders,
            Periods: report.Periods
                .Select(p => new RevenueByPeriodToolDto(
                    p.Period,
                    p.Revenue,
                    p.Discount,
                    p.Tax,
                    p.NetRevenue,
                    p.OrderCount))
                .ToArray());

        return AiToolExecutionResult.Ok(safeReport, 1);
    }
}
