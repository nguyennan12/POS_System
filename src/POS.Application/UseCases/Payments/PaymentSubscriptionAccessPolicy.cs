using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Employees;
using POS.Domain.Orders;
using POS.Domain.Rbac.Constants;

namespace POS.Application.UseCases.Payments;

public sealed class PaymentSubscriptionAccessPolicy(IEmployeeRepository employees)
    : IPaymentSubscriptionAccessPolicy
{
    public async Task<bool> CanAccessShiftAsync(Employee employee, Order order, Shift shift, CancellationToken ct)
    {
        if (employee.StoreId != order.StoreId || shift.StoreId != order.StoreId || shift.Id != order.ShiftId)
            return false;
        if (shift.EmployeeId == employee.Id) return true;
        if (order.CreatedBy != employee.Id) return false;

        // A cashier may observe their own order in a shift opened on behalf of the terminal.
        // Load the opener with its role rather than trusting role names from the caller's token.
        var opener = await employees.GetByIdAsync(shift.EmployeeId, ct);
        return opener is { IsActive: true } && (opener.IsChainOwner ||
            (opener.StoreId == order.StoreId && opener.Role is { IsSystemRole: true, StoreId: null } role &&
                role.Id == opener.RoleId && role.Name is RoleNames.StoreManager or RoleNames.Owner));
    }
}
