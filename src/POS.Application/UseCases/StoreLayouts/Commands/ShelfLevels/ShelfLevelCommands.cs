using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.StoreLayouts.Commands.Zones;

namespace POS.Application.UseCases.StoreLayouts.Commands.ShelfLevels;

public sealed record AddShelfLevelCommand(
    Guid LayoutId,
    Guid ShelfId,
    int LevelNumber,
    string? Label,
    int SortOrder,
    int ExpectedRevision) : ICommand<ShelfLevelDto>, IRequirePermission
{
    public string RequiredPermission => "store_layouts:manage";
}

public sealed record UpdateShelfLevelCommand(
    Guid LayoutId,
    Guid ShelfId,
    Guid ShelfLevelId,
    string? Label,
    int SortOrder,
    int ExpectedRevision) : ICommand<ShelfLevelDto>, IRequirePermission
{
    public string RequiredPermission => "store_layouts:manage";
}

public sealed record DeleteShelfLevelCommand(
    Guid LayoutId,
    Guid ShelfId,
    Guid ShelfLevelId,
    int ExpectedRevision) : ICommand<LayoutMutationResponseDto>, IRequirePermission
{
    public string RequiredPermission => "store_layouts:manage";
}
