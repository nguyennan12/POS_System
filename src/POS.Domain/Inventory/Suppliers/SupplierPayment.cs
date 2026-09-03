using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Inventory.Enums;
using POS.Domain.Inventory.StockIn;

namespace POS.Domain.Inventory.Suppliers;

public class SupplierPayment : BaseEntity
{
    public SupplierPayment() : base()
    {
    }

  public Guid SupplierId { get; private set; }
  public Supplier Supplier { get; private set; } = default!;

  public Guid? VoucherId { get; private set; }
  public StockInVoucher? Voucher { get; private set; }

  public decimal Amount { get; private set; }
  public SupplierPaymentMethod Method { get; private set; }
  public DateTime PaidAt { get; private set; } = DateTime.UtcNow;
  public Guid CreatedBy { get; private set; }
  public Employee CreatedByEmployee { get; private set; } = default!;
  public string? Note { get; private set; }
}
