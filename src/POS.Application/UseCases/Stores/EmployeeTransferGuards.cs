using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Stores.Errors;
using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Stores;

namespace POS.Application.UseCases.Stores;

// Shared data guards for T19 assignment and T20 profile transfers; caller hierarchy stays in each use case.
public static class EmployeeTransferGuards
{
    public static Error ValidateState(Employee employee, Store destination)
    {
        if (!destination.IsActive) return StoreErrors.InactiveStore;
        if (!employee.IsActive || employee.LockedUntil > DateTime.UtcNow) return StoreErrors.InactiveEmployee;
        return Error.None;
    }

    public static async Task<Error> ValidatePinAndShiftAsync(Employee employee, Guid destinationStoreId,
        IEmployeeRepository employees, CancellationToken cancellationToken)
    {
        if (employee.StoreId == destinationStoreId) return Error.None;
        if (await employees.HasOpenShiftOutsideStoreAsync(employee.Id, destinationStoreId, cancellationToken))
            return StoreErrors.EmployeeOpenShift;
        if (string.IsNullOrWhiteSpace(employee.PinLookupHash) ||
            await employees.HasMissingPinLookupAsync(destinationStoreId, cancellationToken))
            return StoreErrors.PinLookupMissing;
        if (await employees.HasPinConflictAsync(employee.Id, destinationStoreId, employee.PinLookupHash, cancellationToken))
            return StoreErrors.EmployeePinAlreadyExists;
        return Error.None;
    }
}
