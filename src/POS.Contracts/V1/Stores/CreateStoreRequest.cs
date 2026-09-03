namespace POS.Contracts.V1.Stores;

public record CreateStoreRequest(
    string Name,
    string? Address,
    string? Phone,
    string Timezone = "Asia/Ho_Chi_Minh",
    string CurrencyCode = "VND"
);
