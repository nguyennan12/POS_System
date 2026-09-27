using POS.Domain.Common;
using POS.Domain.Orders;
using POS.Domain.Promotions;

namespace POS.Application.UseCases.Orders.Services;

public interface ICartCalculationService
{
    Task<Result> RecalculateAsync(
        Order order,
        Voucher? appliedVoucher = null,
        CancellationToken cancellationToken = default,
        bool revalidateVoucher = false);
}
