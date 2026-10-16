using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Orders;
using POS.Domain.Stores;

namespace POS.Application.UseCases.Orders.Services;

public interface ICheckoutGuardService
{
    Task<Result<Employee>> ValidateEmployeeAuthAsync(CancellationToken cancellationToken = default);

    Task<Result<(Shift Shift, Store Store)>> ValidatePreCheckoutGuardsAsync(
        Order order,
        Employee employee,
        CancellationToken cancellationToken = default);

    Task<Result> ValidateInventoryAsync(
        Order order,
        CancellationToken cancellationToken = default);
}
