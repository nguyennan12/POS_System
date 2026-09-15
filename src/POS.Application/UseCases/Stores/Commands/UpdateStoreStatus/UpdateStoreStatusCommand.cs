using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Stores.Queries.GetStoreDetail;

namespace POS.Application.UseCases.Stores.Commands.UpdateStoreStatus;

public record UpdateStoreStatusCommand(Guid StoreId, bool? IsActive) : ICommand<StoreDetailDto>;
