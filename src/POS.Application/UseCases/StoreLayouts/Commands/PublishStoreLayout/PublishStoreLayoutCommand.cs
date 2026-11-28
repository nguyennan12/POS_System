using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.StoreLayouts.Commands.PublishStoreLayout;

public sealed record PublishStoreLayoutCommand(
    Guid LayoutId,
    int ExpectedRevision) : ICommand<StoreLayoutDetailDto>, IRequirePermission
{
    public string RequiredPermission => "store_layouts:approve";
}
