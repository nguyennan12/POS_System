using FluentValidation;
using POS.Application.UseCases.Products.Commands.BulkImportProducts;

namespace POS.Application.UseCases.Products.Validators;

/// <summary>
/// FluentValidation cho BulkImportProductsCommand.
/// Kiểm tra file upload ở tầng Application (defense-in-depth sau Controller):
///   - FileContent không rỗng (null / length = 0)
///   - Kích thước tối đa 10 MB
///   - Extension phải là .xlsx hoặc .xls
/// Validation nghiệp vụ từng dòng Excel được thực hiện bên trong Handler.
///
/// [BUG-5] Fix: Tách riêng rule NotNull và Must cho FileName để tránh null bypass
/// qua điều kiện "name == null || ..." trong rule extension check.
/// </summary>
public class BulkImportProductsCommandValidator : AbstractValidator<BulkImportProductsCommand>
{
    /// <summary>Kích thước tối đa 10 MB.</summary>
    private const int MaxFileSizeBytes = 10 * 1024 * 1024;

    public BulkImportProductsCommandValidator()
    {
        // ── FileContent rules ──────────────────────────────────────────────────
        RuleFor(x => x.FileContent)
            .NotNull()
                .WithMessage("File Excel không được để trống.")
            .Must(b => b is { Length: > 0 })
                .WithMessage("File Excel không được rỗng (0 byte).")
                .When(x => x.FileContent != null)
            .Must(b => b == null || b.Length <= MaxFileSizeBytes)
                .WithMessage($"Kích thước file vượt quá giới hạn {MaxFileSizeBytes / 1024 / 1024} MB.")
                .When(x => x.FileContent != null);

        // ── FileName rules ─────────────────────────────────────────────────────
        // [BUG-5] NotNull() trước Must() — đảm bảo null không bypass check extension.
        RuleFor(x => x.FileName)
            .NotNull()
                .WithMessage("Thiếu tên file.")
            .NotEmpty()
                .WithMessage("Tên file không được để trống.")
                .When(x => x.FileName != null)
            // Chỉ kiểm tra extension khi FileName không null/rỗng
            .Must(name => name != null &&
                          (name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                           name.EndsWith(".xls",  StringComparison.OrdinalIgnoreCase)))
                .WithMessage("Chỉ chấp nhận file Excel (.xlsx, .xls).")
                .When(x => !string.IsNullOrWhiteSpace(x.FileName));
    }
}
