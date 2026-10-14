using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Contracts.V1.Reports;

namespace POS.Application.UseCases.Reports.Commands.ExportReport;

public record ExportReportCommand(
    string Type,
    string Format,
    Dictionary<string, string>? Params = null
) : ICommand<ExportReportJobResponse>, IRequirePermission
{
    public string RequiredPermission => "reports:export";
}
