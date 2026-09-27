using POS.Application.Abstractions.Payments;
using POS.Application.UseCases.Orders.Errors;
using POS.Domain.Common;
using POS.Domain.Orders.Enums;

namespace POS.Application.UseCases.Payments;

public class PaymentStrategyFactory : IPaymentStrategyFactory
{
    private readonly Dictionary<PaymentMethod, IPaymentStrategy> _strategies;

    public PaymentStrategyFactory(IEnumerable<IPaymentStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.Method);
    }

    public IPaymentStrategy GetStrategy(PaymentMethod method)
    {
        if (_strategies.TryGetValue(method, out var strategy))
            return strategy;

        throw new NotSupportedException($"Phương thức thanh toán '{method}' chưa được đăng ký trong hệ thống.");
    }

    public Result<PaymentMethod> ParseMethod(string methodString)
    {
        if (!Enum.TryParse<PaymentMethod>(methodString, ignoreCase: true, out var method)
            || !Enum.IsDefined(typeof(PaymentMethod), method))
        {
            return OrderErrors.InvalidPaymentMethod;
        }

        return Result<PaymentMethod>.Success(method);
    }
}
