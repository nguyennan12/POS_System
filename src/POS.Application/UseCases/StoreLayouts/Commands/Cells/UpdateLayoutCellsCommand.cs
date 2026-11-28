using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.StoreLayouts.Commands.Cells;

public sealed record UpdateLayoutCellsCommand(
    Guid LayoutId,
    IReadOnlyList<LayoutCellChangeDto> Cells,
    int ExpectedRevision) : ICommand<StoreLayoutDetailDto>, IRequirePermission
{
    public string RequiredPermission => "store_layouts:manage";
}
