namespace POS.Contracts.V1.Reports;

public record RevenueReportFilterRequest(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    Guid? StoreId = null,
    Guid? EmployeeId = null
);

public record TopSellingReportFilterRequest(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Limit = 5
);

public record InventoryReportFilterRequest(
    Guid? StoreId = null,
    Guid? CategoryId = null
);

public record ExportReportRequest(
    string Type,
    string Format,
    Dictionary<string, string>? Params = null
);
