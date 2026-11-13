using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Inventory.StockIn.Commands.ImportStockInVoucher;

public record ImportStockInVoucherCommand(
    Guid? StoreId,
    Guid SupplierId,
    byte[] FileContent,
    string? FileName = null,
    string? Note = null,
    bool AutoComplete = true
) : ICommand<ImportStockInVoucherResultDto>, IRequirePermission
{
    public string RequiredPermission => "stock_in_vouchers:manage";
}

public record ImportStockInVoucherResultDto(
    Guid? VoucherId,
    string? VoucherCode,
    int TotalRows,
    int SuccessCount,
    int FailureCount,
    decimal TotalAmount,
    IReadOnlyList<string> Errors
);
