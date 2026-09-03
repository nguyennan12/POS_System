using POS.Domain.Common;

namespace POS.Domain.Inventory.Suppliers;

public class Supplier : BaseEntity
{
    public Supplier() : base()
    {
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
}
