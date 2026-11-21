using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.StoreLayouts.Commands.Zones;

public sealed record CreateZoneCommand(
    Guid LayoutId,
    string Code,
    string Name,
    string Color,
    int PositionX,
    int PositionY,
    int Width,
    int Height,
    int ExpectedRevision) : ICommand<StoreZoneDto>, IRequirePermission
{
    public string RequiredPermission => "store_layouts:manage";
}

public sealed record UpdateZoneCommand(
    Guid LayoutId,
    Guid ZoneId,
    string Code,
    string Name,
    string Color,
    int PositionX,
    int PositionY,
    int Width,
    int Height,
    int ExpectedRevision) : ICommand<StoreZoneDto>, IRequirePermission
{
    public string RequiredPermission => "store_layouts:manage";
}

public sealed record DeleteZoneCommand(
    Guid LayoutId,
    Guid ZoneId,
    int ExpectedRevision) : ICommand<LayoutMutationResponseDto>, IRequirePermission
{
    public string RequiredPermission => "store_layouts:manage";
}

public sealed record LayoutMutationResponseDto(Guid LayoutId, int NewRevision);
