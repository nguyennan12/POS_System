
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Stores.Commands.CreateStore;

public record CreateStoreCommand(
    string Name,
    string? Address,
    string? Phone,
    string Timezone,
    string CurrencyCode
) : ICommand<CreateStoreDto>;

public record CreateStoreDto(
    Guid Id,
    string Name,
    string? Address,
    bool IsActive,
    string? Phone = null,
    string Timezone = "Asia/Ho_Chi_Minh",
    string CurrencyCode = "VND"
);
