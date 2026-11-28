using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.StoreLayouts.Commands.ArchiveStoreLayout;

public sealed record ArchiveStoreLayoutCommand(
    Guid LayoutId,
    int ExpectedRevision) : ICommand<StoreLayoutDetailDto>, IRequirePermission
{
    public string RequiredPermission => "store_layouts:manage";
}
