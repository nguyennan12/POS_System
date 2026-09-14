using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.Common;
using POS.Application.UseCases.Stores.Queries.GetStoreDetail;
using POS.Domain.Common;

namespace POS.Application.UseCases.Stores.Commands.UpdateStore;

public class UpdateStoreCommandHandler(IStoreRepository stores, IEmployeeRepository employees,
    IEmployeeStoreAccessRepository access, ICurrentUser currentUser, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateStoreCommand, StoreDetailDto>
{
    public async Task<Result<StoreDetailDto>> Handle(UpdateStoreCommand command, CancellationToken cancellationToken)
    {
        var caller = await StoreManagementAccess.GetOwnerAsync(currentUser, employees, cancellationToken);
        if (caller.IsFailure) return caller.Error;
        if (!await StoreManagementAccess.CanAccessAsync(caller.Value!, command.StoreId, access, cancellationToken))
            return StoreManagementAccess.Forbidden;

        var store = await stores.GetByIdAsync(command.StoreId, cancellationToken);
        if (store is null) return CommonErrors.NotFound("Store");

        store.UpdateInfo(command.Name, command.Address, command.Phone, command.Timezone,
            command.CurrencyCode, command.TaxCode, command.ReceiptHeader, command.ReceiptFooter);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return StoreDetailDto.FromStore(store);
    }
}
