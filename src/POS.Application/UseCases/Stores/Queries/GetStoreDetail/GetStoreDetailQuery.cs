
using POS.Application.Abstractions.Messaging;
using POS.Domain.Stores;

namespace POS.Application.UseCases.Stores.Queries.GetStoreDetail;

public record GetStoreDetailQuery(
  Guid StoreId) : IQuery<StoreDetailDto>;

public record StoreDetailDto(
  Guid Id,
  string Name,
  string? Address,
  bool IsActive,
  string? Phone,
  string Timezone,
  string CurrencyCode,
  string? TaxCode,
  string? ReceiptHeader,
  string? ReceiptFooter,
  DateTimeOffset CreatedAt,
  DateTimeOffset UpdatedAt)
{
  public static StoreDetailDto FromStore(Store store) => new(
      store.Id, store.Name, store.Address, store.IsActive, store.Phone,
      store.Timezone, store.CurrencyCode, store.TaxCode, store.ReceiptHeader,
      store.ReceiptFooter, new DateTimeOffset(store.CreatedAt), new DateTimeOffset(store.UpdatedAt));
}
