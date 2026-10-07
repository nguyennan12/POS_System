using System;
using POS.Domain.Common;

namespace POS.Domain.Stores;

public class PosRegister : BaseEntity
{
    public PosRegister() : base()
    {
    }

    public PosRegister(
        Guid storeId,
        string name,
        string code,
        bool isActive = true,
        Guid? id = null)
        : base(id)
    {
        StoreId = storeId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Code = code ?? throw new ArgumentNullException(nameof(code));
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid StoreId { get; private set; }
    public Store Store { get; private set; } = default!;

    public string Name { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public void UpdateInfo(string name, string code, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
