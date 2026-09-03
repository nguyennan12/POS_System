
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Stores;

namespace POS.Application.UseCases.Stores.Commands.CreateStore;

public class CreateStoreCommandHandler : ICommandHandler<CreateStoreCommand, CreateStoreDto>
{
  private readonly IStoreRepository _storeRepository;
  private readonly IUnitOfWork _unitOfWork;

  public CreateStoreCommandHandler(IStoreRepository storeRepository, IUnitOfWork unitOfWork)
  {
    _storeRepository = storeRepository;
    _unitOfWork = unitOfWork;
  }

  public async Task<Result<CreateStoreDto>> Handle(
        CreateStoreCommand command,
        CancellationToken cancellationToken)
  {
    var store = new Store(
      command.Name,
      command.Address,
      command.Phone,
      command.Timezone,
      command.CurrencyCode,
      isActive: true);

    await _storeRepository.AddAsync(store, cancellationToken);

    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return new CreateStoreDto(
      store.Id,
      store.Name,
      store.Address,
      store.IsActive,
      store.Phone,
      store.Timezone,
      store.CurrencyCode);
  }

}
