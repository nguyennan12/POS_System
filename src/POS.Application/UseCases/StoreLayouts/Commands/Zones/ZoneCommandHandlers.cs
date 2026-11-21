using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.StoreLayouts;

namespace POS.Application.UseCases.StoreLayouts.Commands.Zones;

public class CreateZoneCommandHandler : ICommandHandler<CreateZoneCommand, StoreZoneDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public CreateZoneCommandHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IUnitOfWork unitOfWork,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _unitOfWork = unitOfWork;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task<Result<StoreZoneDto>> Handle(CreateZoneCommand request, CancellationToken cancellationToken)
    {
        var layout = await _storeLayoutRepository.GetByIdAsync(request.LayoutId, includeGraph: true, cancellationToken);
        if (layout == null)
            return Result<StoreZoneDto>.Failure(StoreLayoutErrors.NotFound);

        if (!_storeAccessPolicy.CanAccessStore(layout.StoreId))
            return Result<StoreZoneDto>.Failure(StoreLayoutErrors.StoreMismatch);

        var now = DateTime.UtcNow;
        var zone = new StoreZone(
            layout.Id,
            request.Code,
            request.Name,
            request.Color,
            request.PositionX,
            request.PositionY,
            request.Width,
            request.Height,
            now);

        var addResult = layout.AddZone(zone, request.ExpectedRevision, now);
        if (addResult.IsFailure)
            return Result<StoreZoneDto>.Failure(addResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<StoreZoneDto>.Success(new StoreZoneDto(
            zone.Id,
            zone.LayoutId,
            zone.Code,
            zone.Name,
            zone.Color,
            zone.PositionX,
            zone.PositionY,
            zone.Width,
            zone.Height));
    }
}

public class UpdateZoneCommandHandler : ICommandHandler<UpdateZoneCommand, StoreZoneDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public UpdateZoneCommandHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IUnitOfWork unitOfWork,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _unitOfWork = unitOfWork;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task<Result<StoreZoneDto>> Handle(UpdateZoneCommand request, CancellationToken cancellationToken)
    {
        var layout = await _storeLayoutRepository.GetByIdAsync(request.LayoutId, includeGraph: true, cancellationToken);
        if (layout == null)
            return Result<StoreZoneDto>.Failure(StoreLayoutErrors.NotFound);

        if (!_storeAccessPolicy.CanAccessStore(layout.StoreId))
            return Result<StoreZoneDto>.Failure(StoreLayoutErrors.StoreMismatch);

        var now = DateTime.UtcNow;
        var updateResult = layout.UpdateZone(
            request.ZoneId,
            request.Code,
            request.Name,
            request.Color,
            request.PositionX,
            request.PositionY,
            request.Width,
            request.Height,
            request.ExpectedRevision,
            now);

        if (updateResult.IsFailure)
            return Result<StoreZoneDto>.Failure(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var zone = layout.Zones.First(z => z.Id == request.ZoneId);
        return Result<StoreZoneDto>.Success(new StoreZoneDto(
            zone.Id,
            zone.LayoutId,
            zone.Code,
            zone.Name,
            zone.Color,
            zone.PositionX,
            zone.PositionY,
            zone.Width,
            zone.Height));
    }
}

public class DeleteZoneCommandHandler : ICommandHandler<DeleteZoneCommand, LayoutMutationResponseDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;

    public DeleteZoneCommandHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IUnitOfWork unitOfWork,
        IStoreAccessPolicy storeAccessPolicy)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _unitOfWork = unitOfWork;
        _storeAccessPolicy = storeAccessPolicy;
    }

    public async Task<Result<LayoutMutationResponseDto>> Handle(DeleteZoneCommand request, CancellationToken cancellationToken)
    {
        var layout = await _storeLayoutRepository.GetByIdAsync(request.LayoutId, includeGraph: true, cancellationToken);
        if (layout == null)
            return Result<LayoutMutationResponseDto>.Failure(StoreLayoutErrors.NotFound);

        if (!_storeAccessPolicy.CanAccessStore(layout.StoreId))
            return Result<LayoutMutationResponseDto>.Failure(StoreLayoutErrors.StoreMismatch);

        var removeResult = layout.RemoveZone(request.ZoneId, request.ExpectedRevision, DateTime.UtcNow);
        if (removeResult.IsFailure)
            return Result<LayoutMutationResponseDto>.Failure(removeResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<LayoutMutationResponseDto>.Success(new LayoutMutationResponseDto(layout.Id, layout.Revision));
    }
}
