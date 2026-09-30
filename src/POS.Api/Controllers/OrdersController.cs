using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Api.Extensions;
using POS.Api.Mappings;
using POS.Application.UseCases.Orders.Commands.AddOrderItem;
using POS.Application.UseCases.Orders.Commands.ApplyVoucher;
using POS.Application.UseCases.Orders.Commands.CancelOrder;
using POS.Application.UseCases.Orders.Commands.CheckoutOrder;
using POS.Application.UseCases.Orders.Commands.CreateOrder;
using POS.Application.UseCases.Orders.Queries.GetOrderById;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Orders;
using POS.Contracts.V1.Payments;
using POS.Domain.Common;

namespace POS.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/orders")]
public class OrdersController(ISender mediator) : ControllerBase
{
  [HttpGet("{id:guid}/payments/{paymentId:guid}/status")]
  public async Task<ActionResult<ApiResponse<PaymentStatusResponse>>> GetPaymentStatus(
      Guid id, Guid paymentId, CancellationToken cancellationToken)
  {
    // Reuse the order query's existing authentication and store access checks.
    var result = await mediator.Send(new GetOrderByIdQuery(id), cancellationToken);
    if (result.IsFailure) return this.ToActionResult(result);
    var payment = result.Value!.Payments.FirstOrDefault(p => p.Id == paymentId);
    if (payment is null)
      return this.ToActionResult(Result.Failure(new Error(ErrorType.NotFound,
          "PAYMENT.NOT_FOUND", "Không tìm thấy thanh toán của đơn hàng.")));

    return Ok(ApiResponse<PaymentStatusResponse>.Ok(new(
        payment.Id, id, payment.Method, payment.Amount, payment.Status,
        payment.TransactionRef, payment.PaidAt)));
  }

  /// <summary>
  /// Tạo mới đơn hàng ở trạng thái Draft gắn với ca làm việc.
  /// </summary>
  [HttpPost]
  public async Task<ActionResult<ApiResponse<OrderDetailResponse>>> Create(
      [FromBody] CreateOrderRequest request,
      CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
        new CreateOrderCommand(request.ShiftId, request.CustomerId),
        cancellationToken);

    if (result.IsFailure) return this.ToActionResult(result);
    return CreatedAtAction(nameof(GetById),
        new { id = result.Value!.Id },
        ApiResponse<OrderDetailResponse>.Ok(result.Value!.ToResponse()));
  }

  /// <summary>
  /// Thêm hoặc cập nhật số lượng SKU vào giỏ hàng và tự động tính lại khuyến mãi, thuế, tổng tiền.
  /// </summary>
  [HttpPost("{id:guid}/items")]
  public async Task<ActionResult<ApiResponse<OrderDetailResponse>>> AddItem(
      Guid id,
      [FromBody] AddOrderItemRequest request,
      CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
        new AddOrderItemCommand(id, request.SkuId, request.Qty),
        cancellationToken);

    if (result.IsFailure) return this.ToActionResult(result);
    return Ok(ApiResponse<OrderDetailResponse>.Ok(result.Value!.ToResponse()));
  }

  /// <summary>
  /// Áp dụng voucher giảm giá vào đơn hàng và tự động tính lại toàn bộ giỏ hàng.
  /// </summary>
  [HttpPost("{id:guid}/vouchers")]
  public async Task<ActionResult<ApiResponse<OrderDetailResponse>>> ApplyVoucher(
      Guid id,
      [FromBody] ApplyVoucherRequest request,
      CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
        new ApplyVoucherCommand(id, request.Code),
        cancellationToken);

    if (result.IsFailure) return this.ToActionResult(result);
    return Ok(ApiResponse<OrderDetailResponse>.Ok(result.Value!.ToResponse()));
  }

  [HttpPost("{id:guid}/checkout")]
  public async Task<ActionResult<ApiResponse<CheckoutResponse>>> Checkout(
      Guid id,
      [FromBody] CheckoutOrderRequest request,
      CancellationToken cancellationToken)
  {
    var paymentInputs = request.Payments.Select(p =>
        new PaymentSplitInputDto(p.Method, p.Amount, p.TransactionRef)).ToList();

    var result = await mediator.Send(
        new CheckoutOrderCommand(id, paymentInputs, request.CustomerId),
        cancellationToken);

    if (result.IsFailure) return this.ToActionResult(result);
    return Ok(ApiResponse<CheckoutResponse>.Ok(result.Value!.ToResponse()));
  }

  /// <summary>
  /// Hủy đơn hàng. Yêu cầu quyền StoreManager hoặc Owner.
  /// </summary>
  [HttpPost("{id:guid}/cancel")]
  public async Task<ActionResult<ApiResponse<OrderDetailResponse>>> Cancel(
      Guid id,
      [FromBody] CancelOrderRequest request,
      CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
        new CancelOrderCommand(id, request.Reason),
        cancellationToken);

    if (result.IsFailure) return this.ToActionResult(result);
    return Ok(ApiResponse<OrderDetailResponse>.Ok(result.Value!.ToResponse()));
  }

  /// <summary>
  /// Lấy chi tiết đơn hàng theo ID.
  /// </summary>
  [HttpGet("{id:guid}")]
  public async Task<ActionResult<ApiResponse<OrderDetailResponse>>> GetById(
      Guid id,
      CancellationToken cancellationToken)
  {
    var result = await mediator.Send(
        new GetOrderByIdQuery(id),
        cancellationToken);

    if (result.IsFailure) return this.ToActionResult(result);
    return Ok(ApiResponse<OrderDetailResponse>.Ok(result.Value!.ToResponse()));
  }
}
