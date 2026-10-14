using POS.Application.UseCases.Customers;
using POS.Application.UseCases.Inventory;

namespace POS.Application.UseCases.Orders.Commands.ProcessOrderPaid;

public record OrderPaidSideEffectsDto(
    IReadOnlyList<StockTransactionDto> StockTransactions,
    IReadOnlyList<StockAlertDto> StockAlerts,
    PointTransactionDto? PointTransaction
);
