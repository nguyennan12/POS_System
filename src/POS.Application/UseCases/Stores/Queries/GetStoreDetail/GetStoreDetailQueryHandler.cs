using MediatR;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.Common;
using POS.Domain.Common;

namespace POS.Application.UseCases.Stores.Queries.GetStoreDetail;

public class GetStoreDetailQueryHandler : IQueryHandler<GetStoreDetailQuery, StoreDetailDto>
{
  private readonly IStoreRepository _storeRepository;

  public GetStoreDetailQueryHandler(IStoreRepository storeRepository)
  {
    _storeRepository = storeRepository;
  }



  public async Task<Result<StoreDetailDto>> Handle(GetStoreDetailQuery query, CancellationToken cancellationToken)
  {
    var store = await _storeRepository.GetByIdAsync(query.StoreId, cancellationToken);

    if (store is null)
      return CommonErrors.NotFound("Store");

    return new StoreDetailDto(
        store.Id,
        store.Name,
        store.Address,
        store.IsActive);

  }
}
