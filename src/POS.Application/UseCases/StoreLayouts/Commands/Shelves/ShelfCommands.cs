using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.StoreLayouts.Commands.Zones;
using POS.Domain.StoreLayouts.Enums;

namespace POS.Application.UseCases.StoreLayouts.Commands.Shelves;

public sealed record CreateShelfCommand(
    Guid LayoutId,
    Guid? ZoneId,
    string Code,
    string Name,
    int PositionX,
    int PositionY,
    int Width,
    int Height,
    ShelfOrientation Orientation,
    int ExpectedRevision) : ICommand<ShelfDto>, IRequirePermission
{
    public string RequiredPermission => "store_layouts:manage";
}

public sealed record UpdateShelfCommand(
    Guid LayoutId,
    Guid ShelfId,
    Guid? ZoneId,
    string Code,
    string Name,
    int PositionX,
    int PositionY,
    int Width,
    int Height,
    ShelfOrientation Orientation,
    ShelfStatus Status,
    int ExpectedRevision) : ICommand<ShelfDto>, IRequirePermission
{
    public string RequiredPermission => "store_layouts:manage";
}

public sealed record DeleteShelfCommand(
    Guid LayoutId,
    Guid ShelfId,
    int ExpectedRevision) : ICommand<LayoutMutationResponseDto>, IRequirePermission
{
    public string RequiredPermission => "store_layouts:manage";
}
