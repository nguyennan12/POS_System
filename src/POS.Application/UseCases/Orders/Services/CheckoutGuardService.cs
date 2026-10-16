using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Orders.Errors;
using POS.Domain.Common;
using POS.Domain.Customers.Errors;
using POS.Domain.Employees;
using POS.Domain.Employees.Enums;
using POS.Domain.Orders;
using POS.Domain.Orders.Enums;
using POS.Domain.Rbac.Constants;
using POS.Domain.Stores;

namespace POS.Application.UseCases.Orders.Services;

public class CheckoutGuardService(
    ICurrentUser currentUser,
    IEmployeeRepository employeeRepository,
    IShiftRepository shiftRepository,
    IStoreRepository storeRepository,
    ICustomerRepository customerRepository,
    IStockEntryRepository stockEntryRepository) : ICheckoutGuardService
{
    public async Task<Result<Employee>> ValidateEmployeeAuthAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.EmployeeId is null)
            return OrderErrors.Unauthorized;

        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null || !employee.IsActive)
            return OrderErrors.Unauthorized;

        return Result<Employee>.Success(employee);
    }

    public async Task<Result<(Shift Shift, Store Store)>> ValidatePreCheckoutGuardsAsync(
        Order order,
        Employee employee,
        CancellationToken cancellationToken = default)
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
        var shift = await shiftRepository.GetByIdAsync(order.ShiftId, cancellationToken);
        if (shift is null)
            return OrderErrors.ShiftNotFound;

        if (shift.Status != ShiftStatus.Open)
            return OrderErrors.ShiftClosed;

        if (shift.StoreId != order.StoreId)
            return OrderErrors.ShiftStoreMismatch;

        // Phân quyền ca làm việc:
        // 1. Quản lý / Chủ cửa hàng luôn có quyền thanh toán trên mọi ca.
        var isManagerOrAbove = employee.IsChainOwner
            || (employee.Role?.Name is RoleNames.StoreManager or RoleNames.Owner);

        if (!isManagerOrAbove && shift.EmployeeId != employee.Id)
        {
            // 2. Nếu người thanh toán là Thu ngân khác, kiểm tra xem người mở ca có phải là Quản lý/Owner (mở ca hộ quầy) hay không.
            var shiftOpener = shift.Employee ?? await employeeRepository.GetByIdAsync(shift.EmployeeId, cancellationToken);
            var isShiftOpenedByManager = shiftOpener != null && (shiftOpener.IsChainOwner || shiftOpener.Role?.Name is RoleNames.StoreManager or RoleNames.Owner);

            if (!isShiftOpenedByManager)
            {
                // Nếu ca do một Thu ngân khác (Cashier) mở, không cho phép Cashier này thao tác chèn vào két của người khác
                return OrderErrors.ShiftNotOwned;
            }
        }

        // Cửa hàng
        var store = await storeRepository.GetByIdAsync(order.StoreId, cancellationToken);
        if (store is null || !store.IsActive)
            return OrderErrors.StoreInactive;

        // Khách hàng (nếu có)
        if (order.CustomerId.HasValue)
        {
            var customer = order.Customer != null && order.Customer.Id == order.CustomerId.Value
                ? order.Customer
                : (await customerRepository.GetByIdAsync(order.CustomerId.Value, cancellationToken))?.Customer;

            if (customer is null)
                return OrderErrors.CustomerNotFound;

            if (!customer.IsActive)
                return CustomerErrors.Inactive;
        }

        return Result<(Shift, Store)>.Success((shift, store));
    }

    public async Task<Result> ValidateInventoryAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in order.Items)
        {
            var stockEntry = await stockEntryRepository.GetBySkuAndStoreAsync(item.SkuId, order.StoreId, cancellationToken);
            if (stockEntry is null || stockEntry.QtyOnHand < item.Qty)
                return OrderErrors.StockInsufficient;
        }
        return Result.Success();
    }
}
