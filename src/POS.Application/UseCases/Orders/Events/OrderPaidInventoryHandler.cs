using MediatR;
using Microsoft.Extensions.Logging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Inventory.Stock;
using POS.Domain.Orders.Enums;
using POS.Domain.Orders.Events;

namespace POS.Application.UseCases.Orders.Events;

/// <summary>
/// Handles OrderPaidDomainEvent to automatically deduct stock (FEFO), generate StockTransaction SaleOut,
/// and check MinStock threshold for inventory alerts (Task T53).
/// </summary>
public sealed class OrderPaidInventoryHandler(
    IOrderRepository orderRepository,
    IStockEntryRepository stockEntryRepository,
    IStockTransactionRepository stockTransactionRepository,
    ILogger<OrderPaidInventoryHandler> logger) : INotificationHandler<OrderPaidDomainEvent>
{
    public async Task Handle(OrderPaidDomainEvent notification, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdWithDetailsAsync(notification.OrderId, cancellationToken);
        if (order is null || order.Status != OrderStatus.Paid)
            return;

        foreach (var item in order.Items)
        {
            var entry = await stockEntryRepository.GetBySkuAndStoreAsync(item.SkuId, order.StoreId, cancellationToken);
            var remainingQty = (entry?.QtyOnHand ?? 0) - item.Qty;

            await stockEntryRepository.DeductStockWithBatchesAsync(item.SkuId, order.StoreId, item.Qty, cancellationToken);

            var stockTx = StockTransaction.CreateSaleOut(
                storeId: order.StoreId,
                skuId: item.SkuId,
                qty: item.Qty,
                createdBy: order.CreatedBy,
                orderId: order.Id);

            await stockTransactionRepository.AddAsync(stockTx, cancellationToken);

            var minStock = entry?.MinStock ?? 0;
            if (entry != null && remainingQty <= minStock)
            {
                var skuCode = entry.Sku?.SkuCode ?? string.Empty;
                logger.LogWarning(
                    "Cảnh báo tồn kho tối thiểu: SKU {SkuCode} tại cửa hàng {StoreId} còn {RemainingQty} (ngưỡng MinStock: {MinStock})",
                    skuCode, order.StoreId, remainingQty, minStock);
            }
        }
    }
}
