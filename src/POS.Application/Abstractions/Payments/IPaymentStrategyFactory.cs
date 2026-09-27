using POS.Domain.Common;
using POS.Domain.Orders.Enums;

namespace POS.Application.Abstractions.Payments;

public interface IPaymentStrategyFactory
{
    /// <summary>
    /// Lấy Strategy tương ứng với phương thức thanh toán.
    /// </summary>
    IPaymentStrategy GetStrategy(PaymentMethod method);

    /// <summary>
    /// Parse chuỗi phương thức thanh toán sang enum hợp lệ một cách an toàn.
    /// </summary>
    Result<PaymentMethod> ParseMethod(string methodString);
}
