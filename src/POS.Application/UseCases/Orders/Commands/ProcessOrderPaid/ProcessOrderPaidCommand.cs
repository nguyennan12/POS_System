using POS.Application.Abstractions.Messaging;
using POS.Domain.Orders;

namespace POS.Application.UseCases.Orders.Commands.ProcessOrderPaid;

/// <summary>
/// Internal command triggered when an order transitions to Paid:
/// Automatically deducts inventory (FEFO), generates StockTransactions (SaleOut),
/// evaluates MinStock alerts, and accrues loyalty points + updates tier spending for customers.
/// Executed within the caller's Unit of Work.
/// </summary>
public sealed record ProcessOrderPaidCommand(
    Guid OrderId,
    Guid EmployeeId,
    Order? Order = null
) : ICommand<OrderPaidSideEffectsDto>;
