using Microsoft.Extensions.Logging;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Customers;
using POS.Application.UseCases.Inventory;
using POS.Application.UseCases.Orders.Errors;
using POS.Domain.Common;
using POS.Domain.Customers;
using POS.Domain.Customers.Enums;
using POS.Domain.Inventory.Stock;
using POS.Domain.Orders.Enums;

namespace POS.Application.UseCases.Orders.Commands.ProcessOrderPaid;

/// <summary>
/// Executes automatic inventory deduction (FEFO), generates StockTransaction SaleOut,
/// checks MinStock alerts, and triggers customer loyalty point accrual + spending tracking on Order Paid.
/// </summary>
public sealed class ProcessOrderPaidCommandHandler(
    IOrderRepository orderRepository,
    IStockEntryRepository stockEntryRepository,
    IStockTransactionRepository stockTransactionRepository,
    ICustomerRepository customerRepository,
    IMemberTierRepository memberTierRepository,
    ILogger<ProcessOrderPaidCommandHandler> logger)
    : ICommandHandler<ProcessOrderPaidCommand, OrderPaidSideEffectsDto>
{
    public async Task<Result<OrderPaidSideEffectsDto>> Handle(
        ProcessOrderPaidCommand command,
        CancellationToken cancellationToken)
    {
        var order = command.Order ?? await orderRepository.GetByIdWithDetailsAsync(command.OrderId, cancellationToken);
        if (order is null)
            return OrderErrors.OrderNotFound;

        if (order.Status != OrderStatus.Paid)
            return OrderErrors.OrderNotPaid;

        var stockTransactions = new List<StockTransactionDto>();
        var stockAlerts = new List<StockAlertDto>();
        PointTransactionDto? pointTxDto = null;

        // 1. Tự động trừ tồn kho (FEFO) và tạo StockTransaction SaleOut, kiểm tra MinStock
        foreach (var item in order.Items)
        {
            var entry = await stockEntryRepository.GetBySkuAndStoreAsync(item.SkuId, order.StoreId, cancellationToken);
            var remainingQty = (entry?.QtyOnHand ?? 0) - item.Qty;

            await stockEntryRepository.DeductStockWithBatchesAsync(item.SkuId, order.StoreId, item.Qty, cancellationToken);

            var stockTx = StockTransaction.CreateSaleOut(
                storeId: order.StoreId,
                skuId: item.SkuId,
                qty: item.Qty,
                createdBy: command.EmployeeId,
                orderId: order.Id);
            await stockTransactionRepository.AddAsync(stockTx, cancellationToken);

            stockTransactions.Add(new StockTransactionDto(
                stockTx.Id,
                stockTx.StoreId,
                stockTx.SkuId,
                entry?.Sku?.SkuCode ?? string.Empty,
                stockTx.Type.ToString(),
                stockTx.Qty,
                entry?.AverageCost,
                stockTx.OrderId,
                null,
                stockTx.Note,
                stockTx.CreatedBy,
                new DateTimeOffset(stockTx.CreatedAt, TimeSpan.Zero)
            ));

            // Kích hoạt cảnh báo nếu kho xuống dưới mức MinStock
            var minStock = entry?.MinStock ?? 0;
            if (entry != null && remainingQty <= minStock)
            {
                var skuCode = entry.Sku?.SkuCode ?? string.Empty;
                var barcode = entry.Sku?.Barcode ?? string.Empty;
                var productName = entry.Sku?.Product?.Name ?? string.Empty;

                var alertDto = new StockAlertDto(
                    item.SkuId,
                    skuCode,
                    barcode,
                    productName,
                    remainingQty,
                    minStock,
                    "MinStock"
                );
                stockAlerts.Add(alertDto);

                logger.LogWarning(
                    "Cảnh báo tồn kho tối thiểu: SKU {SkuCode} tại cửa hàng {StoreId} còn {RemainingQty} (ngưỡng MinStock: {MinStock})",
                    skuCode, order.StoreId, remainingQty, minStock);
            }
        }

        // 2. Kích hoạt cộng điểm thành viên & ghi nhận doanh số tích lũy
        if (order.CustomerId.HasValue)
        {
            var customer = await customerRepository.GetEntityByIdAsync(order.CustomerId.Value, cancellationToken);
            if (customer != null && customer.IsActive)
            {
                var availableTiers = await memberTierRepository.GetAllAsync(cancellationToken);
                customer.RecordSpending(order.GrandTotal, availableTiers);

                var tier = customer.MemberTier ?? availableTiers.FirstOrDefault(t => t.Id == customer.MemberTierId);
                var rate = tier?.PointRate ?? 0.01m;
                var pointsEarned = Math.Round(order.GrandTotal * rate, 0, MidpointRounding.AwayFromZero);

                if (pointsEarned > 0)
                {
                    var loyaltyAccount = await customerRepository.GetLoyaltyAccountWithTierAsync(customer.Id, cancellationToken);
                    if (loyaltyAccount is null)
                    {
                        loyaltyAccount = new LoyaltyAccount(customer.Id, 0);
                        await customerRepository.AddLoyaltyAccountAsync(loyaltyAccount, cancellationToken);
                    }

                    loyaltyAccount.AddPoints(pointsEarned);

                    var pointTx = new PointTransaction(
                        customerId: customer.Id,
                        points: pointsEarned,
                        type: PointTransactionType.Earn,
                        orderId: order.Id,
                        note: $"Tích điểm từ đơn hàng #{order.Id}"
                    );
                    await customerRepository.AddPointTransactionAsync(pointTx, cancellationToken);

                    pointTxDto = new PointTransactionDto(
                        pointTx.Id,
                        pointTx.CustomerId,
                        pointTx.Points,
                        pointTx.Type.ToString(),
                        pointTx.OrderId,
                        pointTx.Note,
                        new DateTimeOffset(pointTx.CreatedAt, TimeSpan.Zero)
                    );
                }
            }
        }

        return Result<OrderPaidSideEffectsDto>.Success(
            new OrderPaidSideEffectsDto(stockTransactions, stockAlerts, pointTxDto));
    }
}
