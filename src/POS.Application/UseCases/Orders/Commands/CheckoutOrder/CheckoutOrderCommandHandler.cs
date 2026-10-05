using MediatR;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.Abstractions.Payments;
using POS.Application.UseCases.Invoices.Commands.GenerateInvoice;
using POS.Application.UseCases.Orders.DTOs;
using POS.Application.UseCases.Orders.Errors;
using POS.Application.UseCases.Orders.Mappings;
using POS.Application.UseCases.Orders.Services;
using POS.Domain.Common;
using POS.Domain.Customers;
using POS.Domain.Customers.Enums;
using POS.Domain.Customers.Errors;
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
    IStoreRepository storeRepository,
    IStockEntryRepository stockEntryRepository,
    IStockTransactionRepository stockTransactionRepository,
    ISender sender,
    IVoucherRepository voucherRepository,
    IVoucherUsageRepository voucherUsageRepository,
    IPaymentStrategyFactory paymentStrategyFactory,
    ICartCalculationService cartCalculationService,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ICustomerRepository customerRepository) : ICommandHandler<CheckoutOrderCommand, CheckoutDto>
{

  /// Validates and processes checkout in a serializable transaction, recording point redemptions and applying side effects when the order is paid.
  /// </summary>
  public async Task<Result<CheckoutDto>> Handle(
      CheckoutOrderCommand command,
      CancellationToken cancellationToken)
  {
    try
    {
      // Replay the complete decision after a PostgreSQL serialization conflict.
      // UnitOfWork rolls back and clears tracked entities before the next attempt.
      for (var attempt = 0; ; attempt++)
      {
        var result = await unitOfWork.ExecuteSerializableAsync<CheckoutDto>(async ct =>
        {
          var authResult = await ValidateEmployeeAuthAsync(ct);
          if (authResult.IsFailure) return authResult.Error;
          var employee = authResult.Value!;

          var order = await orderRepository.GetByIdWithDetailsAsync(command.OrderId, ct);
          if (order is null) return OrderErrors.OrderNotFound;

          if (command.CustomerId.HasValue && order.CustomerId != command.CustomerId.Value)
          {
            if (order.Status != OrderStatus.Draft)
              return OrderErrors.NotDraft;
            order.SetCustomer(command.CustomerId.Value);
          }

          var guardResult = await ValidatePreCheckoutGuardsAsync(order, employee, ct);
          if (guardResult.IsFailure) return guardResult.Error;
          var (_, store) = guardResult.Value;

          var prepareResult = await PrepareAndRecalculateOrderAsync(order, ct);
          if (prepareResult.IsFailure) return prepareResult.Error;

          var paymentResult = await ParseAndValidatePaymentsAsync(command.Payments, order, ct);
          if (paymentResult.IsFailure) return paymentResult.Error;
          var parsedPayments = paymentResult.Value!;

          var stockResult = await ValidateInventoryAsync(order, ct);
          if (stockResult.IsFailure) return stockResult.Error;

          // Redeem only this request's points, including partial settlements.
          var points = parsedPayments.Where(p => p.Method == PaymentMethod.Points).Sum(p => p.Amount);
          if (points > 0)
          {
            var account = await customerRepository.GetLoyaltyAccountAsync(order.CustomerId!.Value, ct);
            if (account is null || !account.DeductPoints(points))
              return OrderErrors.InsufficientPoints;

            var pointTx = new PointTransaction(
                customerId: order.CustomerId!.Value,
                points: points,
                type: PointTransactionType.Redeem,
                orderId: order.Id,
                note: "Thanh toán điểm cho đơn hàng"
            );
            await customerRepository.AddPointTransactionAsync(pointTx, ct);
          }

          var previousIds = order.Payments.Select(p => p.Id).ToHashSet();
          order.ProcessPayments(parsedPayments);
          await orderRepository.AddPaymentsAsync(order.Payments.Where(p => !previousIds.Contains(p.Id)), ct);

          if (order.Status == OrderStatus.Paid)
          {
            await ExecutePostPaidSideEffectsAsync(order, employee, ct);
            var invoiceResult = await sender.Send(new GenerateInvoiceCommand(order.Id), ct);
            if (invoiceResult.IsFailure) return invoiceResult.Error;
          }

          await unitOfWork.SaveChangesAsync(ct);
          return Result<CheckoutDto>.Success(order.ToCheckoutDto(employee, store));
        }, cancellationToken);
        if (attempt < 2 && result.IsFailure && result.Error.Code == "Persistence.ConcurrentModification")
          continue;
        return result;
      }
    }
    catch (PersistenceConflictException ex) when (ex.ConstraintName == "IX_payments_method_transaction_ref")
    {
      // Includes a duplicate that won a concurrent race after our existence check.
      return OrderErrors.DuplicatePayment;
    }
    catch (PersistenceConflictException ex) when (ex.ConstraintName is
        "IX_invoices_invoice_no" or "IX_invoices_order_id")
    {
      // Database uniqueness remains the final guard if another writer bypasses
      // the application precheck or an invoice number was inserted externally.
      return new Error(ErrorType.Invalid, "Persistence.ConcurrentModification",
          "Hóa đơn đã thay đổi đồng thời. Hãy tải lại đơn hàng và thử lại.");
    }
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

    // Khách hàng (nếu có)
    if (order.CustomerId.HasValue)
    {
      var customer = order.Customer != null && order.Customer.Id == order.CustomerId.Value
          ? order.Customer
          : (await customerRepository.GetByIdAsync(order.CustomerId.Value, ct))?.Customer;

      if (customer is null)
        return OrderErrors.CustomerNotFound;

      if (!customer.IsActive)
        return CustomerErrors.Inactive;
    }

    return Result<(Shift, Store)>.Success((shift, store));
  }

  private async Task<Result<List<(PaymentMethod Method, decimal Amount, string? TransactionRef)>>> ParseAndValidatePaymentsAsync(
      IReadOnlyList<PaymentSplitInputDto> payments, Order order, CancellationToken ct)
  {
    if (payments is null)
      return OrderErrors.NoPaymentsProvided;

    if (payments.Count == 0 && order.GrandTotal > 0)
      return OrderErrors.NoPaymentsProvided;

    if (order.Payments.Any(p => !Enum.IsDefined(p.Status) || !Enum.IsDefined(p.Method)
        || p.Amount <= 0 || p.Amount != Math.Round(p.Amount, 2)
        || (p.ChangeAmount.HasValue && (p.ChangeAmount < 0 || p.ChangeAmount > p.Amount
            || p.ChangeAmount != Math.Round(p.ChangeAmount.Value, 2)))
        || (p.Method != PaymentMethod.Cash && (p.ChangeAmount ?? 0) != 0)))
      return OrderErrors.InvalidPaymentState;

    var parsedPayments = new List<(PaymentMethod Method, decimal Amount, string? TransactionRef)>();
    var remaining = order.GrandTotal - order.GetPaymentTotals().TotalApplied;
    if (remaining < 0) return OrderErrors.InvalidPaymentState;
    if (remaining == 0 && (order.Payments.Any(p => p.Status == PaymentStatus.Success) || order.Status == OrderStatus.Paid))
      return OrderErrors.AlreadyPaid;
    var references = order.Payments.Where(p => p.TransactionRef != null)
        .Select(p => (p.Method, p.TransactionRef)).ToHashSet();

    foreach (var p in payments)
    {
      if (p is null || p.Amount <= 0 || p.Amount != Math.Round(p.Amount, 2) || p.Amount > 9999999999999999.99m)
        return OrderErrors.InvalidPaymentAmount;
      if (p.TransactionRef != null && (string.IsNullOrWhiteSpace(p.TransactionRef) || p.TransactionRef.Length > 100))
        return OrderErrors.InvalidTransactionRef;

      var parseResult = paymentStrategyFactory.ParseMethod(p.Method);
      if (parseResult.IsFailure)
        return parseResult.Error;

      var method = parseResult.Value;
      IPaymentStrategy strategy;
      try
      {
        strategy = paymentStrategyFactory.GetStrategy(method);
      }
      catch (NotSupportedException)
      {
        return OrderErrors.InvalidPaymentMethod;
      }

      var validationResult = await strategy.ValidateAsync(p, order, ct);
      if (validationResult.IsFailure)
        return validationResult.Error;

      if (p.TransactionRef != null && (!references.Add((method, p.TransactionRef))
          || await orderRepository.PaymentReferenceExistsAsync(method, p.TransactionRef, ct)))
        return OrderErrors.DuplicatePayment;

      if (method != PaymentMethod.Cash && p.Amount > remaining)
        return OrderErrors.NonCashOverpaymentNotAllowed;

      remaining -= Math.Min(p.Amount, remaining);

      parsedPayments.Add((method, p.Amount, p.TransactionRef));
    }

    return Result<List<(PaymentMethod, decimal, string?)>>.Success(parsedPayments);
  }

  private async Task<Result> PrepareAndRecalculateOrderAsync(Order order, CancellationToken ct)
  {
    // Confirmed orders retain the total agreed before the first payment.
    if (order.Status == OrderStatus.Confirmed)
      return Result.Success();

    if (order.Status != OrderStatus.Draft)
    {
      return order.Status == OrderStatus.Paid ? OrderErrors.AlreadyPaid : OrderErrors.AlreadyCancelled;
    }

    decimal grandTotalBefore = order.GrandTotal;
    var previousDiscounts = order.Discounts.ToList();
    var recalcResult = await cartCalculationService.RecalculateAsync(order, revalidateVoucher: true, cancellationToken: ct);
    if (recalcResult.IsFailure)
      return recalcResult.Error;

    if (order.GrandTotal != grandTotalBefore)
      return OrderErrors.GrandTotalMismatch;

    orderRepository.ReplaceDiscounts(previousDiscounts, order.Discounts);
    return order.Confirm();
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

  private async Task ExecutePostPaidSideEffectsAsync(Order order, Employee employee, CancellationToken ct)
  {
    // 1. Trừ kho (kèm theo lô FEFO nếu có) và tạo StockTransaction SaleOut
    foreach (var item in order.Items)
    {
      await stockEntryRepository.DeductStockWithBatchesAsync(item.SkuId, order.StoreId, item.Qty, ct);

      var stockTx = StockTransaction.CreateSaleOut(
          storeId: order.StoreId,
          skuId: item.SkuId,
          qty: item.Qty,
          createdBy: employee.Id,
          orderId: order.Id);
      await stockTransactionRepository.AddAsync(stockTx, ct);
    }

    // 2. Ghi nhận sử dụng voucher
    if (order.AppliedVoucherId.HasValue)
    {
      var voucher = await voucherRepository.GetByIdWithPromotionAsync(order.AppliedVoucherId.Value, ct);
      if (voucher != null)
      {
        voucher.RecordUse();
        if (order.CustomerId.HasValue)
        {
          var usage = VoucherUsage.Create(
              voucherId: voucher.Id,
              customerId: order.CustomerId.Value,
              orderId: order.Id);
          await voucherUsageRepository.AddAsync(usage, ct);
        }
      }
    }

    // 3. Thực thi xử lý post-paid cho các phương thức thanh toán qua Strategy
    foreach (var payment in order.Payments.Where(p => p.Status == PaymentStatus.Success))
    {
      var strategy = paymentStrategyFactory.GetStrategy(payment.Method);
      await strategy.ProcessPostPaidAsync(payment, order, ct);
    }
  }
}
