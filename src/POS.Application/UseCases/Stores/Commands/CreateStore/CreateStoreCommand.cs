
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
    string Name
);
