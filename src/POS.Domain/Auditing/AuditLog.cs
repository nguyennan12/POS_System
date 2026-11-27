using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Stores;

namespace POS.Domain.Auditing;

public class AuditLog : BaseEntity
{
    public AuditLog() : base()
    {
    }

    public AuditLog(Guid? actorId, Guid targetEmployeeId, Guid? storeId, string action) : base()
    {
        EmployeeId = actorId;
        EntityType = "Employee";
        EntityId = targetEmployeeId;
        StoreId = storeId;
        Action = action;
    }

    public static AuditLog Authentication(Employee? employee, string action) =>
        new(employee?.Id, employee?.Id ?? Guid.Empty, employee?.StoreId, action);

    public static AuditLog SystemPayment(Guid paymentId, Guid storeId, string action) => new()
    {
        EmployeeId = null,
        StoreId = storeId,
        EntityType = "Payment",
        EntityId = paymentId,
        Action = action,
        Description = "Actor=System"
    };

    public static AuditLog QrPaymentRequested(Guid paymentId, Guid storeId, Guid? actorId) => new()
    {
        EmployeeId = actorId, StoreId = storeId, EntityType = "Payment",
        EntityId = paymentId, Action = "QrPaymentRequested"
    };

    public static AuditLog AiToolExecution(
        Guid executionId,
        Guid? employeeId,
        Guid? storeId,
        string toolName,
        bool success,
        long durationMs,
        int resultCount,
        string? errorCode = null)
    {
        var trimmedTool = string.IsNullOrWhiteSpace(toolName) ? "unknown" : toolName.Trim();
        var safeAction = trimmedTool.Length > 50 ? trimmedTool[..50] : trimmedTool;
        var clampedDuration = Math.Max(0L, durationMs);

        string description;
        if (success)
        {
            description = $"Status=Success; DurationMs={clampedDuration}; ResultCount={Math.Max(0, resultCount)}";
        }
        else
        {
            var safeErrorCode = string.IsNullOrWhiteSpace(errorCode) ? "UNKNOWN_ERROR" : errorCode.Trim();
            description = $"Status=Failure; DurationMs={clampedDuration}; ErrorCode={safeErrorCode}";
        }

        if (description.Length > 500)
        {
            description = description[..500];
        }

        return new AuditLog
        {
            EmployeeId = employeeId,
            StoreId = storeId,
            EntityType = "AiTool",
            EntityId = executionId == Guid.Empty ? Guid.NewGuid() : executionId,
            Action = safeAction,
            Description = description
        };
    }

    public Guid? StoreId { get; private set; }
    public Store? Store { get; private set; }

    public Guid? EmployeeId { get; private set; }
    public Employee? Employee { get; private set; }

    public string Action { get; private set; } = default!;
    public string EntityType { get; private set; } = default!;
    public Guid EntityId { get; private set; }
    public string? Description { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}
