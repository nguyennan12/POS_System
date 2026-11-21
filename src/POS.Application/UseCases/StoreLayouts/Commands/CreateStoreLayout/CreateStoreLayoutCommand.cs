using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.StoreLayouts.Commands.CreateStoreLayout;

public sealed record CreateStoreLayoutCommand(
    string Name,
    int GridWidth,
    int GridHeight,
    Guid? StoreId = null) : ICommand<StoreLayoutDetailDto>, IRequirePermission
{
    public string RequiredPermission => "store_layouts:manage";
}
