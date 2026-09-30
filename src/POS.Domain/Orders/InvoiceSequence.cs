namespace POS.Domain.Orders;

// One transactionally updated counter per store and local business date.
public class InvoiceSequence
{
    public Guid StoreId { get; private set; }
    public DateOnly InvoiceDate { get; private set; }
    public long LastValue { get; private set; }
}
