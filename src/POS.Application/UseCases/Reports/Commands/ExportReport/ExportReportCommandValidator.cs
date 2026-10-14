using FluentValidation;

namespace POS.Application.UseCases.Reports.Commands.ExportReport;

public class ExportReportCommandValidator : AbstractValidator<ExportReportCommand>
{
    private static readonly string[] AllowedTypes = ["revenue", "inventory", "top-selling", "slow-moving", "shift"];
    private static readonly string[] AllowedFormats = ["xlsx", "excel", "pdf"];

    public ExportReportCommandValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Loại báo cáo không được để trống.")
            .Must(t => AllowedTypes.Contains(t.ToLowerInvariant()))
            .WithMessage($"Loại báo cáo không hợp lệ. Các loại cho phép: {string.Join(", ", AllowedTypes)}.");

        RuleFor(x => x.Format)
            .NotEmpty().WithMessage("Định dạng xuất không được để trống.")
            .Must(f => AllowedFormats.Contains(f.ToLowerInvariant()))
            .WithMessage($"Định dạng không hợp lệ. Các định dạng cho phép: {string.Join(", ", AllowedFormats)}.");
    }
}
