using POS.Domain.Common;
using POS.Domain.Rbac;
using POS.Domain.Stores;

namespace POS.Domain.Employees;

public class Employee : BaseEntity
{
    public Employee() : base()
    {
    }

    public Employee(
        string name,
        string username,
        string passwordHash,
        string pinHash,
        Guid roleId,
        bool isChainOwner = false,
        Guid? storeId = null,
        bool isActive = true,
        Guid? id = null)
        : base(id)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Username = username ?? throw new ArgumentNullException(nameof(username));
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        PinHash = pinHash ?? throw new ArgumentNullException(nameof(pinHash));
        RoleId = roleId;
        IsChainOwner = isChainOwner;
        StoreId = storeId;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid? StoreId { get; private set; }
    public Store? Store { get; private set; }

    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = default!;

    public bool IsChainOwner { get; private set; }

    public string Name { get; private set; } = default!;
    public string Username { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string PinHash { get; private set; } = default!;
    public string? PinLookupHash { get; private set; }
    public short FailedLoginCount { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
}
