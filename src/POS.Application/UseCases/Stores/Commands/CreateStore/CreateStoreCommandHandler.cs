
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Stores;
using POS.Domain.Employees;
using POS.Application.Abstractions.Auth;

namespace POS.Application.UseCases.Stores.Commands.CreateStore;

public class CreateStoreCommandHandler(IStoreRepository storeRepository, IUnitOfWork unitOfWork,
    IEmployeeRepository employees, IEmployeeStoreAccessRepository access, ICurrentUser currentUser)
    : ICommandHandler<CreateStoreCommand, CreateStoreDto>
{

  public async Task<Result<CreateStoreDto>> Handle(
        CreateStoreCommand command,
        CancellationToken cancellationToken)
  {
    var caller = await StoreManagementAccess.GetOwnerAsync(currentUser, employees, cancellationToken);
    if (caller.IsFailure) return caller.Error;
    if (!caller.Value!.IsChainOwner) return StoreManagementAccess.Forbidden;

    var store = new Store(
      command.Name,
      command.Address,
      command.Phone,
      command.Timezone,
      command.CurrencyCode,
      isActive: true);

    await storeRepository.AddAsync(store, cancellationToken);
    // The creator must be able to read/manage the new store; save both rows atomically.
    await access.AddAsync(new EmployeeStoreAccess(caller.Value.Id, store.Id, caller.Value.Id), cancellationToken);
    await unitOfWork.SaveChangesAsync(cancellationToken);

    return new CreateStoreDto(
      store.Id,
      store.Name,
      store.Address,
      store.IsActive,
      store.Phone,
      store.Timezone,
      store.CurrencyCode,
      new DateTimeOffset(store.CreatedAt),
      new DateTimeOffset(store.UpdatedAt));
  }

}
