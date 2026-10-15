using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Contracts.V1.Config;

namespace POS.Application.UseCases.Config.Commands.UpdateStoreConfig;

public record UpdateStoreConfigCommand(Guid? StoreId, Dictionary<string, string> Configs)
    : ICommand<StoreConfigResponse>, IRequirePermission
{
    public string RequiredPermission => "stores:update";
}
