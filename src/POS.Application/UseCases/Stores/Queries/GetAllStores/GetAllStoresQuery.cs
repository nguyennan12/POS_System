using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Stores.Queries.GetAllStores;

public record GetAllStoresQuery : IQuery<List<StoreDto>>;
public record StoreDto(Guid Id, string Name, string? Address, string? Phone, bool IsActive);
