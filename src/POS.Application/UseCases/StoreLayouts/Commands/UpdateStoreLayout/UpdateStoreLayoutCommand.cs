using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.StoreLayouts.Commands.UpdateStoreLayout;

public sealed record UpdateStoreLayoutCommand(
    Guid LayoutId,
    string Name,
    int ExpectedRevision) : ICommand<StoreLayoutDetailDto>, IRequirePermission
{
    public string RequiredPermission => "store_layouts:manage";
}
