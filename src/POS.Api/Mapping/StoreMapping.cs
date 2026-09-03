using POS.Application.UseCases.Stores.Queries.GetStoreDetail;
using POS.Application.UseCases.Stores.Commands.CreateStore;
using POS.Contracts.V1.Stores;

namespace POS.Api.Mappings;

public static class StoreMapping
{
  public static StoreDetailResponse ToResponse(
 this StoreDetailDto store)
  {
    return new StoreDetailResponse(
    store.Id,
    store.Name,
    store.Address,
    store.IsActive);
  }

  public static StoreDetailResponse ToResponse(
      this CreateStoreDto store)
  {
    return new StoreDetailResponse(
        store.Id,
        store.Name,
        store.Address,
        store.IsActive,
        store.Phone,
        store.Timezone,
        store.CurrencyCode
      );
  }

}
