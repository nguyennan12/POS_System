using POS.Domain.Employees;
using POS.Domain.Orders;

namespace POS.Application.Abstractions.Auth;

public interface IPaymentSubscriptionAccessPolicy
{
    Task<bool> CanAccessShiftAsync(Employee employee, Order order, Shift shift, CancellationToken ct);
}
