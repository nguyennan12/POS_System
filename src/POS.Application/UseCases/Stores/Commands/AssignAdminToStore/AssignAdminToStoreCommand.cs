using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Stores.Queries.GetStoreDetail;

namespace POS.Application.UseCases.Stores.Commands.AssignAdminToStore;

public record AssignAdminToStoreCommand(Guid StoreId, Guid EmployeeId) : ICommand<StoreDetailDto>;
