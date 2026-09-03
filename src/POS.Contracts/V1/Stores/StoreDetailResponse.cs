namespace POS.Contracts.V1.Stores;

public record StoreDetailResponse(
    Guid Id,
    string Name,
    string? Address,
    bool IsActive
);
