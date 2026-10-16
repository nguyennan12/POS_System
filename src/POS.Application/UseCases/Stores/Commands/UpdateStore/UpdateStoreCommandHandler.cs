using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Stores.Errors;
using POS.Application.UseCases.Stores.Queries.GetStoreDetail;
using POS.Domain.Common;

namespace POS.Application.UseCases.Stores.Commands.UpdateStore;

public class UpdateStoreCommandHandler(IStoreRepository stores, IStoreAccessPolicy storeAccessPolicy,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateStoreCommand, StoreDetailDto>
{
    public async Task<Result<StoreDetailDto>> Handle(UpdateStoreCommand command, CancellationToken cancellationToken)
    {
        var caller = await storeAccessPolicy.GetAuthenticatedOwnerAsync(cancellationToken);
        if (caller.IsFailure) return caller.Error;
        if (!storeAccessPolicy.CanAccessStore(command.StoreId))
            return StoreErrors.Forbidden;

        var store = await stores.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null) return StoreErrors.StoreNotFound;

        store.UpdateInfo(command.Name, command.Address, command.Phone, command.Timezone,
            command.CurrencyCode, command.TaxCode, command.ReceiptHeader, command.ReceiptFooter);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return StoreDetailDto.FromStore(store);
    }
}
