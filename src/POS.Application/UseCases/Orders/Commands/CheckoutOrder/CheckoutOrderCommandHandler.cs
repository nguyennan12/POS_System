using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.Abstractions.Payments;
using POS.Application.UseCases.Orders.DTOs;
using POS.Application.UseCases.Orders.Errors;
using POS.Application.UseCases.Orders.Services;
using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Employees.Enums;
using POS.Domain.Inventory.Stock;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;
using POS.Domain.Promotions;
using POS.Domain.Rbac.Constants;
using POS.Domain.Stores;

namespace POS.Application.UseCases.Orders.Commands.CheckoutOrder;

public class CheckoutOrderCommandHandler(
    IOrderRepository orderRepository,
    IShiftRepository shiftRepository,
    IEmployeeRepository employeeRepository,
    IEmployeeStoreAccessRepository employeeStoreAccessRepository,
    IStoreRepository storeRepository,
    IStockEntryRepository stockEntryRepository,
    IStockTransactionRepository stockTransactionRepository,
    IInvoiceRepository invoiceRepository,
    IVoucherRepository voucherRepository,
    IVoucherUsageRepository voucherUsageRepository,
    IPaymentStrategyFactory paymentStrategyFactory,
    ICartCalculationService cartCalculationService,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : ICommandHandler<CheckoutOrderCommand, CheckoutDto>
{
    public async Task<Result<CheckoutDto>> Handle(
        CheckoutOrderCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Kiểm tra xác thực nhân viên
        var authResult = await ValidateEmployeeAuthAsync(cancellationToken);
        if (authResult.IsFailure) return authResult.Error;
        var employee = authResult.Value!;

        // 2. Lấy thông tin đơn hàng
        var order = await orderRepository.GetByIdWithDetailsAsync(command.OrderId, cancellationToken);
        if (order is null) return OrderErrors.OrderNotFound;

        // 3. Kiểm tra trạng thái đơn, ca làm việc, quyền cửa hàng và trạng thái cửa hàng
        var guardResult = await ValidatePreCheckoutGuardsAsync(order, employee, cancellationToken);
        if (guardResult.IsFailure) return guardResult.Error;
        var (shift, store) = guardResult.Value;

        // 4. Validate các phương thức thanh toán đầu vào qua Payment Strategy Factory
        var paymentResult = await ParseAndValidatePaymentsAsync(command.Payments, order, cancellationToken);
        if (paymentResult.IsFailure) return paymentResult.Error;
        var parsedPayments = paymentResult.Value!;

        // 5. Thực thi thanh toán trong Serializable Transaction
        return await unitOfWork.ExecuteSerializableAsync(async ct =>
        {
            var freshOrder = await orderRepository.GetByIdWithDetailsAsync(command.OrderId, ct);
            if (freshOrder is null) return OrderErrors.OrderNotFound;

            var prepareResult = await PrepareAndRecalculateOrderAsync(freshOrder, ct);
            if (prepareResult.IsFailure) return prepareResult.Error;

            var stockResult = await ValidateInventoryAsync(freshOrder, ct);
            if (stockResult.IsFailure) return stockResult.Error;

            freshOrder.ProcessPayments(parsedPayments);
            orderRepository.Update(freshOrder);

            if (freshOrder.Status == OrderStatus.Paid)
            {
                await ExecutePostPaidSideEffectsAsync(freshOrder, employee, store, ct);
            }

            await unitOfWork.SaveChangesAsync(ct);

            return Result<CheckoutDto>.Success(BuildCheckoutDto(freshOrder, employee, store));
        }, cancellationToken);
    }

    // ── Helper Methods ────────────────────────────────────────────────────────

    private async Task<Result<Employee>> ValidateEmployeeAuthAsync(CancellationToken ct)
    {
        if (currentUser.EmployeeId is null)
            return OrderErrors.Unauthorized;

        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, ct);
        if (employee is null || !employee.IsActive)
            return OrderErrors.Unauthorized;

        return Result<Employee>.Success(employee);
    }

    private async Task<Result<(Shift Shift, Store Store)>> ValidatePreCheckoutGuardsAsync(
        Order order, Employee employee, CancellationToken ct)
    {
        if (order.Status == OrderStatus.Paid)
            return OrderErrors.AlreadyPaid;

        if (order.Status == OrderStatus.Cancelled)
            return OrderErrors.AlreadyCancelled;

        if (order.Items.Count == 0)
            return OrderErrors.CartEmpty;

        // Quyền truy cập cửa hàng
        if (!employee.IsChainOwner && employee.StoreId != order.StoreId)
        {
            var hasAccess = await employeeStoreAccessRepository.ExistsAsync(employee.Id, order.StoreId, ct);
            if (!hasAccess)
                return OrderErrors.InvalidStore;
        }

        // Ca làm việc
        var shift = await shiftRepository.GetByIdAsync(order.ShiftId, ct);
        if (shift is null)
            return OrderErrors.ShiftNotFound;

        if (shift.Status != ShiftStatus.Open)
            return OrderErrors.ShiftClosed;

        if (shift.StoreId != order.StoreId)
            return OrderErrors.ShiftStoreMismatch;

        var isManagerOrAbove = employee.IsChainOwner
            || (employee.Role?.Name is RoleNames.StoreManager or RoleNames.Owner);
        if (!isManagerOrAbove && shift.EmployeeId != employee.Id)
            return OrderErrors.ShiftNotOwned;

        // Cửa hàng
        var store = await storeRepository.GetByIdAsync(order.StoreId, ct);
        if (store is null || !store.IsActive)
            return OrderErrors.StoreInactive;

        return Result<(Shift, Store)>.Success((shift, store));
    }

    private async Task<Result<List<(PaymentMethod Method, decimal Amount, string? TransactionRef)>>> ParseAndValidatePaymentsAsync(
        IReadOnlyList<PaymentSplitInputDto> payments, Order order, CancellationToken ct)
    {
        if (payments.Count == 0)
            return OrderErrors.NoPaymentsProvided;

        var parsedPayments = new List<(PaymentMethod Method, decimal Amount, string? TransactionRef)>();
        decimal nonCashTotal = 0;

        foreach (var p in payments)
        {
            var parseResult = paymentStrategyFactory.ParseMethod(p.Method);
            if (parseResult.IsFailure)
                return parseResult.Error;

            var method = parseResult.Value;
            var strategy = paymentStrategyFactory.GetStrategy(method);

            var validationResult = await strategy.ValidateAsync(p, order, ct);
            if (validationResult.IsFailure)
                return validationResult.Error;

            if (method != PaymentMethod.Cash)
                nonCashTotal += p.Amount;

            parsedPayments.Add((method, p.Amount, p.TransactionRef));
        }

        if (nonCashTotal > order.GrandTotal)
            return OrderErrors.NonCashOverpaymentNotAllowed;

        return Result<List<(PaymentMethod, decimal, string?)>>.Success(parsedPayments);
    }

    private async Task<Result> PrepareAndRecalculateOrderAsync(Order order, CancellationToken ct)
    {
        if (order.Status == OrderStatus.Draft)
        {
            var confirmResult = order.Confirm();
            if (confirmResult.IsFailure)
                return confirmResult.Error;
        }
        else if (order.Status != OrderStatus.Confirmed)
        {
            return order.Status == OrderStatus.Paid ? OrderErrors.AlreadyPaid : OrderErrors.AlreadyCancelled;
        }

        decimal grandTotalBefore = order.GrandTotal;
        var recalcResult = await cartCalculationService.RecalculateAsync(order, revalidateVoucher: true, cancellationToken: ct);
        if (recalcResult.IsFailure)
            return recalcResult.Error;

        if (order.GrandTotal != grandTotalBefore)
            return OrderErrors.GrandTotalMismatch;

        return Result.Success();
    }

    private async Task<Result> ValidateInventoryAsync(Order order, CancellationToken ct)
    {
        foreach (var item in order.Items)
        {
            var stockEntry = await stockEntryRepository.GetBySkuAndStoreAsync(item.SkuId, order.StoreId, ct);
            if (stockEntry is null || stockEntry.QtyOnHand < item.Qty)
                return OrderErrors.StockInsufficient;
        }
        return Result.Success();
    }

    private async Task ExecutePostPaidSideEffectsAsync(Order order, Employee employee, Store store, CancellationToken ct)
    {
        // 1. Trừ kho và tạo StockTransaction SaleOut
        foreach (var item in order.Items)
        {
            await stockEntryRepository.DeductStockAsync(item.SkuId, order.StoreId, item.Qty, ct);

            var stockTx = StockTransaction.CreateSaleOut(
                storeId: order.StoreId,
                skuId: item.SkuId,
                qty: item.Qty,
                createdBy: employee.Id,
                orderId: order.Id);
            await stockTransactionRepository.AddAsync(stockTx, ct);
        }

        // 2. Ghi nhận sử dụng voucher
        if (order.AppliedVoucherId.HasValue && order.CustomerId.HasValue)
        {
            var voucher = await voucherRepository.GetByIdWithPromotionAsync(order.AppliedVoucherId.Value, ct);
            if (voucher != null)
            {
                voucher.RecordUse();
                var usage = VoucherUsage.Create(
                    voucherId: voucher.Id,
                    customerId: order.CustomerId.Value,
                    orderId: order.Id);
                await voucherUsageRepository.AddAsync(usage, ct);
            }
        }

        // 3. Thực thi xử lý post-paid cho các phương thức thanh toán qua Strategy
        foreach (var payment in order.Payments)
        {
            var strategy = paymentStrategyFactory.GetStrategy(payment.Method);
            await strategy.ProcessPostPaidAsync(payment, order, ct);
        }

        // 4. Tạo Invoice nếu chưa có
        if (!await invoiceRepository.ExistsForOrderAsync(order.Id, ct))
        {
            var seq = await invoiceRepository.GetNextSequenceAsync(order.StoreId, DateTime.UtcNow, ct);
            var storeCode = (store.TaxCode ?? order.StoreId.ToString("N")[..6]).ToUpper();
            var invoiceNo = $"HD-{storeCode}-{DateTime.UtcNow:yyyyMMdd}-{seq:D4}";
            var invoice = Invoice.Create(
                orderId: order.Id,
                invoiceNo: invoiceNo,
                subtotal: order.Subtotal,
                taxAmount: order.TaxTotal,
                grandTotal: order.GrandTotal,
                buyerName: order.Customer?.Name);
            await invoiceRepository.AddAsync(invoice, ct);
        }
    }

    private static CheckoutDto BuildCheckoutDto(Order order, Employee employee, Store store)
    {
        var totalPaid = order.Payments
            .Where(p => p.Status == PaymentStatus.Success)
            .Sum(p => p.Amount);

        var lastCashPayment = order.Payments
            .Where(p => p.Method == PaymentMethod.Cash && p.Status == PaymentStatus.Success)
            .LastOrDefault();

        var changeAmount = lastCashPayment?.ChangeAmount ?? 0;

        ReceiptDataDto? receiptData = null;
        if (order.Status == OrderStatus.Paid)
        {
            var itemsDto = order.Items.Select(i => new OrderItemDto(
                Id: i.Id,
                SkuId: i.SkuId,
                SkuCode: i.Sku?.SkuCode ?? string.Empty,
                ProductName: i.Sku?.Product?.Name ?? string.Empty,
                Qty: i.Qty,
                UnitPrice: i.UnitPrice,
                DiscountAmount: i.DiscountAmount,
                TaxAmount: i.TaxAmount,
                LineTotal: i.LineTotal
            )).ToList();

            receiptData = new ReceiptDataDto(
                StoreName: order.Store?.Name ?? string.Empty,
                StoreAddress: order.Store?.Address,
                StorePhone: order.Store?.Phone,
                OrderNo: order.Id.ToString("N")[..8].ToUpper(),
                CashierName: employee.Name,
                CreatedAt: new DateTimeOffset(order.CreatedAt, TimeSpan.Zero),
                Items: itemsDto,
                Subtotal: order.Subtotal,
                DiscountTotal: order.DiscountTotal,
                TaxTotal: order.TaxTotal,
                GrandTotal: order.GrandTotal,
                AmountPaid: totalPaid - changeAmount,
                ChangeAmount: changeAmount,
                ReceiptHeader: store.ReceiptHeader,
                ReceiptFooter: store.ReceiptFooter
            );
        }

        var paymentDtos = order.Payments.Select(p => new OrderPaymentDto(
            Id: p.Id,
            Method: p.Method.ToString(),
            Amount: p.Amount,
            ChangeAmount: p.ChangeAmount,
            TransactionRef: p.TransactionRef,
            Status: p.Status.ToString(),
            PaidAt: p.PaidAt.HasValue ? new DateTimeOffset(p.PaidAt.Value, TimeSpan.Zero) : null
        )).ToList();

        return new CheckoutDto(
            OrderId: order.Id,
            GrandTotal: order.GrandTotal,
            TotalPaid: totalPaid - changeAmount,
            ChangeAmount: changeAmount,
            Status: order.Status.ToString(),
            Payments: paymentDtos,
            ReceiptData: receiptData
        );
    }
}
