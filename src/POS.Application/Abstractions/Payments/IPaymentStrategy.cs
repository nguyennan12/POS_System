using POS.Application.UseCases.Orders.Commands.CheckoutOrder;
using POS.Domain.Common;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;

namespace POS.Application.Abstractions.Payments;

public interface IPaymentStrategy
{
  PaymentMethod Method { get; }


  /// Kiểm tra tính hợp lệ của phương thức thanh toán trước khi chấp nhận giao dịch.
  /// </summary>
  Task<Result> ValidateAsync(PaymentSplitInputDto payment, Order order, CancellationToken cancellationToken = default);


  /// Thực hiện các tác vụ phát sinh sau khi đơn hàng thanh toán thành công (Paid)
  /// ví dụ: trừ điểm khách hàng, gọi webhook xác nhận,...
  /// </summary>
  Task ProcessPostPaidAsync(Payment payment, Order order, CancellationToken cancellationToken = default);
}
