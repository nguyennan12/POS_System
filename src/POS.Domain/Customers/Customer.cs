using POS.Domain.Common;

namespace POS.Domain.Customers;

public class Customer : BaseEntity
{
    public Customer() : base()
    {
    }

    public Customer(
        string name,
        string phone,
        Guid memberTierId,
        string? email = null,
        DateOnly? dob = null,
        string? barcode = null,
        bool isActive = true,
        Guid? id = null) : base(id)
    {
        Name = name;
        Phone = phone;
        MemberTierId = memberTierId;
        Email = email;
        Dob = dob;
        Barcode = barcode;
        IsActive = isActive;
        TotalSpending = 0;
        CreatedAt = DateTime.UtcNow;
    }

    public string Name { get; private set; } = default!;
    public string Phone { get; private set; } = default!;
    public string? Email { get; private set; }
    public DateOnly? Dob { get; private set; }
    public string? Barcode { get; private set; }
    public Guid MemberTierId { get; private set; }
    public MemberTier MemberTier { get; private set; } = default!;
    public decimal TotalSpending { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public void Update(string name, string phone, string? email, DateOnly? dob, string? barcode, bool isActive)
    {
        Name = name;
        Phone = phone;
        Email = email;
        Dob = dob;
        Barcode = barcode;
        IsActive = isActive;
    }

    public void SetMemberTier(Guid newTierId)
    {
        MemberTierId = newTierId;
    }

    public bool RecordSpending(decimal amount, IEnumerable<MemberTier> availableTiers)
    {
        if (amount <= 0) return false;
        TotalSpending += amount;
        return EvaluateTierUpgrade(availableTiers);
    }

    public bool EvaluateTierUpgrade(IEnumerable<MemberTier> availableTiers)
    {
        var eligibleTier = availableTiers
            .OrderByDescending(t => t.MinSpending)
            .FirstOrDefault(t => TotalSpending >= t.MinSpending);

        if (eligibleTier != null && eligibleTier.Id != MemberTierId)
        {
            MemberTierId = eligibleTier.Id;
            MemberTier = eligibleTier;
            return true;
        }

        return false;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
