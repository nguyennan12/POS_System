
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Stores.Queries.GetStoreDetail;

public record GetStoreDetailQuery(
  Guid StoreId) : IQuery<StoreDetailDto>;

public record StoreDetailDto(
  Guid Id,
  string Name,
  string? Address,
  bool IsActive);
