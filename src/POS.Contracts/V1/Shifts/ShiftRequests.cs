using POS.Contracts.V1.Common;

namespace POS.Contracts.V1.Shifts;

public record ShiftFilterRequest(
    Guid? StoreId = null,
    string? Status = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int PageNumber = 1,
    int PageSize = 20
) : PagedRequest(PageNumber, PageSize);

public record OpenShiftRequest(
    decimal OpeningCash,
    string? Note = null
);

public record CloseShiftRequest(
    decimal ActualCash,
    string? Note = null
);
