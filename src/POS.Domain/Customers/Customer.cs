using POS.Domain.Common;

namespace POS.Domain.Customers;

public class Customer : BaseEntity
{
    public Customer() : base()
    {
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
}
