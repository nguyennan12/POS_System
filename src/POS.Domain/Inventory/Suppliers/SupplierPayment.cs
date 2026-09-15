using POS.Domain.Common;
using POS.Domain.Employees;
using POS.Domain.Inventory.Enums;

namespace POS.Domain.Inventory.Suppliers;

public class SupplierPayment : BaseEntity
{
    /// <summary>EF Core parameterless constructor.</summary>
    public SupplierPayment() : base() { }

    public SupplierPayment(
        Guid supplierId,
        decimal amount,
        SupplierPaymentMethod method,
        Guid createdBy,
        Guid? voucherId = null,
        string? note = null,
        Guid? id = null)
        : base(id)
    {
        SupplierId = supplierId;
        Amount = amount;
        Method = method;
        CreatedBy = createdBy;
        VoucherId = voucherId;
        Note = note;
        PaidAt = DateTime.UtcNow;
    }

    public Guid SupplierId { get; private set; }
    public Supplier Supplier { get; private set; } = default!;

    public Guid? VoucherId { get; private set; }

    public decimal Amount { get; private set; }
    public SupplierPaymentMethod Method { get; private set; }
    public DateTime PaidAt { get; private set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; private set; }
    public Employee CreatedByEmployee { get; private set; } = default!;
    public string? Note { get; private set; }
}
