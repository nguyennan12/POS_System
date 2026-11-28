using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.StoreLayouts.Commands.CloneStoreLayout;

public sealed record CloneStoreLayoutCommand(
    Guid SourceLayoutId,
    Guid? TargetStoreId,
    string NewName,
    bool CopyPlacements,
    int ExpectedSourceRevision) : ICommand<CloneStoreLayoutResultDto>, IRequirePermission
{
    public string RequiredPermission => "store_layouts:manage";
}
