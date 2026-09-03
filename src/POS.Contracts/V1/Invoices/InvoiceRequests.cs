using POS.Contracts.V1.Common;

namespace POS.Contracts.V1.Invoices;

public record InvoiceFilterRequest(
    Guid? OrderId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int PageNumber = 1,
    int PageSize = 20
) : PagedRequest(PageNumber, PageSize);
