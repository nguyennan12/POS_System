namespace POS.Contracts.V1.Stores;

public record CreateStoreRequest(
    string Name,
    string? Address,
    string? Phone,
    string Timezone = "Asia/Ho_Chi_Minh",
    string CurrencyCode = "VND"
);

public record UpdateStoreRequest(
    string Name,
    string? Address,
    string? Phone,
    string Timezone = "Asia/Ho_Chi_Minh",
    string CurrencyCode = "VND",
    string? TaxCode = null,
    string? ReceiptHeader = null,
    string? ReceiptFooter = null
);

public record UpdateStoreStatusRequest(
    bool IsActive
);

public record StoreAdminAssignmentRequest(
    Guid EmployeeId
);

public record StoreOwnerAccessRequest(
    Guid EmployeeId
);
