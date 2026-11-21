using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.StoreLayouts.Commands.Zones;

namespace POS.Application.UseCases.StoreLayouts.Commands.Placements;

public sealed record AssignProductPlacementCommand(
    Guid LayoutId,
    Guid ShelfLevelId,
    Guid SkuId,
    bool IsPrimary,
    string? Note,
    int ExpectedRevision) : ICommand<ProductPlacementDto>, IRequirePermission
{
    public string RequiredPermission => "product_placements:manage";
}

public sealed record MoveProductPlacementCommand(
    Guid LayoutId,
    Guid PlacementId,
    Guid TargetShelfLevelId,
    string? Note,
    int ExpectedRevision) : ICommand<ProductPlacementDto>, IRequirePermission
{
    public string RequiredPermission => "product_placements:manage";
}

public sealed record SetPrimaryPlacementCommand(
    Guid LayoutId,
    Guid PlacementId,
    int ExpectedRevision) : ICommand<ProductPlacementDto>, IRequirePermission
{
    public string RequiredPermission => "product_placements:manage";
}

public sealed record RemoveProductPlacementCommand(
    Guid LayoutId,
    Guid PlacementId,
    string? Note,
    int ExpectedRevision) : ICommand<LayoutMutationResponseDto>, IRequirePermission
{
    public string RequiredPermission => "product_placements:manage";
}
