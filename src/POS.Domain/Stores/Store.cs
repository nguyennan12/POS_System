using POS.Domain.Common;
using POS.Domain.Employees;

namespace POS.Domain.Stores;

public class Store : BaseEntity
{
    public Store() : base()
    {
    }

    public Store(
        string name,
        string? address = null,
        string? phone = null,
        string? timezone = "Asia/Ho_Chi_Minh",
        string? currencyCode = "VND",
        string? taxCode = null,
        string? receiptHeader = null,
        string? receiptFooter = null,
        bool isActive = true,
        Guid? id = null)
        : base(id)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Address = address;
        Phone = phone;
        Timezone = string.IsNullOrWhiteSpace(timezone) ? "Asia/Ho_Chi_Minh" : timezone;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "VND" : currencyCode;
        TaxCode = taxCode;
        ReceiptHeader = receiptHeader;
        ReceiptFooter = receiptFooter;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public string Name { get; private set; } = default!;
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public string Timezone { get; private set; } = "Asia/Ho_Chi_Minh";
    public string CurrencyCode { get; private set; } = "VND";
    public string? TaxCode { get; private set; }
    public string? ReceiptHeader { get; private set; }
    public string? ReceiptFooter { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public ICollection<Employee> Employees { get; private set; } = new List<Employee>();
}
