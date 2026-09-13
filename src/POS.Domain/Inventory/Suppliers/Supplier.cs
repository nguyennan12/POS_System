using POS.Domain.Common;

namespace POS.Domain.Inventory.Suppliers;

public class Supplier : BaseEntity
{
    /// <summary>EF Core parameterless constructor.</summary>
    public Supplier() : base() { }

    public Supplier(
        string name,
        string? taxCode = null,
        string? contactName = null,
        string? phone = null,
        string? email = null,
        string? address = null,
        string? creditTerms = null,
        Guid? id = null)
        : base(id)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        TaxCode = taxCode;
        ContactName = contactName;
        Phone = phone;
        Email = email;
        Address = address;
        CreditTerms = creditTerms;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public string Name { get; private set; } = default!;
    public string? TaxCode { get; private set; }
    public string? ContactName { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string? CreditTerms { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public void Update(
        string name,
        string? taxCode,
        string? contactName,
        string? phone,
        string? email,
        string? address,
        string? creditTerms,
        bool isActive)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        TaxCode = taxCode;
        ContactName = contactName;
        Phone = phone;
        Email = email;
        Address = address;
        CreditTerms = creditTerms;
        IsActive = isActive;
    }

    public void Deactivate() => IsActive = false;
    public void Activate()   => IsActive = true;
}
