using POS.Domain.Common;
using POS.Domain.Rbac;
using POS.Domain.Rbac.Constants;
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

  public bool IsLocked(DateTime utcNow)
  {
    return LockedUntil.HasValue && LockedUntil.Value > utcNow;
  }

  public void RegisterFailedLogin(DateTime utcNow, short maxAttempts = 5, int lockMinutes = 15)
  {
    FailedLoginCount++;
    if (FailedLoginCount >= maxAttempts)
    {
      LockedUntil = utcNow.AddMinutes(lockMinutes);
    }
    UpdatedAt = utcNow;
  }

  public void RegisterSuccessfulLogin(DateTime utcNow)
  {
    FailedLoginCount = 0;
    LockedUntil = null;
    UpdatedAt = utcNow;
  }

  public void setPinLookUpHash(string pinLookupHash)
  {
    PinLookupHash = pinLookupHash;
    UpdatedAt = DateTime.UtcNow;
  }

  public void UpdateProfile(string name, Guid roleId, Guid? storeId, bool isChainOwner)
  {
    if (roleId == Guid.Empty || storeId == Guid.Empty || (!isChainOwner && storeId is null))
      throw new ArgumentException("A role and a store (unless chain owner) are required.");
    Name = name ?? throw new ArgumentNullException(nameof(name));
    RoleId = roleId;
    StoreId = storeId;
    IsChainOwner = isChainOwner;
    UpdatedAt = DateTime.UtcNow;
  }

  public void SetActive(bool isActive)
  {
    IsActive = isActive;
    if (isActive) RegisterSuccessfulLogin(DateTime.UtcNow);
    UpdatedAt = DateTime.UtcNow;
  }

  public void ResetCredentialPassword(string newHash)
  {
    PasswordHash = newHash ?? throw new ArgumentNullException(nameof(newHash));
    RegisterSuccessfulLogin(DateTime.UtcNow);
  }

  public void ResetCredentialPin(string newHash, string newLookupHash)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(newHash);
    ArgumentException.ThrowIfNullOrWhiteSpace(newLookupHash);
    PinHash = newHash;
    PinLookupHash = newLookupHash;
    RegisterSuccessfulLogin(DateTime.UtcNow);
  }

    public void AssignStoreManager(Guid storeId, Role storeManagerRole)
    {
        if (storeId == Guid.Empty || storeManagerRole.Id == Guid.Empty ||
            !storeManagerRole.IsSystemRole || storeManagerRole.StoreId != null ||
            storeManagerRole.Name != RoleNames.StoreManager)
            throw new ArgumentException("A store and the system StoreManager role are required.");
        if (IsChainOwner || !IsActive)
            throw new InvalidOperationException("Only active store employees can be assigned as StoreManager.");
        if (StoreId == storeId && RoleId == storeManagerRole.Id) return;

        StoreId = storeId;
        RoleId = storeManagerRole.Id;
        Role = storeManagerRole;
        UpdatedAt = DateTime.UtcNow;
    }
}
