using POS.Domain.Common;
using POS.Domain.Stores;

namespace POS.Domain.Employees;

public class EmployeeStoreAccess : BaseEntity
{
    public EmployeeStoreAccess() : base()
    {
    }

    public Guid EmployeeId { get; private set; }
    public EmployeeStoreAccess(Guid employeeId, Guid storeId, Guid grantedBy) : base()
    {
        if (employeeId == Guid.Empty || storeId == Guid.Empty || grantedBy == Guid.Empty)
            throw new ArgumentException("Employee, store and grantor IDs must not be empty.");
        EmployeeId = employeeId;
        StoreId = storeId;
        GrantedBy = grantedBy;
        GrantedAt = DateTime.UtcNow;
    }

    public Employee Employee { get; private set; } = default!;

    public Guid StoreId { get; private set; }
    public Store Store { get; private set; } = default!;

    public Guid? GrantedBy { get; private set; }
    public Employee? GrantedByEmployee { get; private set; }
    public DateTime GrantedAt { get; private set; } = DateTime.UtcNow;
}
