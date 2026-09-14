namespace POS.Application.Abstractions.Persistence;

public sealed class EmployeeAssignmentConflictException(Exception innerException)
    : Exception("Employee PIN conflicts with another employee in the destination store.", innerException);
