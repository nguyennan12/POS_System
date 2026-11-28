using POS.Domain.Common;

namespace POS.Domain.Stores;

public sealed class ScannerDevice : BaseEntity
{
    public ScannerDevice() { }

    public ScannerDevice(Guid storeId, Guid registerId, string name, string secretHash)
    {
        StoreId = storeId;
        RegisterId = registerId;
        Name = name.Trim();
        SecretHash = secretHash;
    }

    public Guid StoreId { get; private set; }
    public Guid RegisterId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string SecretHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; private set; }

    public void Rotate(string secretHash) => SecretHash = secretHash;
    public void Revoke()
    {
        IsActive = false;
        RevokedAt = DateTime.UtcNow;
    }
}
