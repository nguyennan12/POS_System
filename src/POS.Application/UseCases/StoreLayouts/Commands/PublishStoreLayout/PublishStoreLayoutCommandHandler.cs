using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.StoreLayouts.Services;
using POS.Domain.Common;

namespace POS.Application.UseCases.StoreLayouts.Commands.PublishStoreLayout;

public class PublishStoreLayoutCommandHandler : ICommandHandler<PublishStoreLayoutCommand, StoreLayoutDetailDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IStoreLayoutValidationService _validationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;
    private readonly ICurrentUser _currentUser;

    public PublishStoreLayoutCommandHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IStoreLayoutValidationService validationService,
        IUnitOfWork unitOfWork,
        IStoreAccessPolicy storeAccessPolicy,
        ICurrentUser currentUser)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _validationService = validationService;
        _unitOfWork = unitOfWork;
        _storeAccessPolicy = storeAccessPolicy;
        _currentUser = currentUser;
    }

    public async Task<Result<StoreLayoutDetailDto>> Handle(PublishStoreLayoutCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteSerializableAsync(async ct =>
        {
            var layout = await _storeLayoutRepository.GetByIdAsync(request.LayoutId, includeGraph: true, ct);
            if (layout == null)
                return Result<StoreLayoutDetailDto>.Failure(StoreLayoutErrors.NotFound);

            if (!_storeAccessPolicy.CanAccessStore(layout.StoreId))
                return Result<StoreLayoutDetailDto>.Failure(StoreLayoutErrors.StoreMismatch);

            var validation = await _validationService.ValidateForPublishAsync(layout, layout.StoreId, ct);
            if (validation.IsFailure)
                return Result<StoreLayoutDetailDto>.Failure(validation.Error);

            var now = DateTime.UtcNow;
            var actorId = _currentUser.EmployeeId ?? Guid.Empty;

            // Archive currently active layout of this store if any (1 store = 1 Active layout at a time).
            var currentActive = await _storeLayoutRepository.GetActiveByStoreAsync(layout.StoreId, includeGraph: false, ct);
            if (currentActive != null && currentActive.Id != layout.Id)
            {
                currentActive.Archive(actorId, now);
            }

            var publishResult = layout.Publish(actorId, request.ExpectedRevision, now);
            if (publishResult.IsFailure)
                return Result<StoreLayoutDetailDto>.Failure(publishResult.Error);

            await _unitOfWork.SaveChangesAsync(ct);
            return Result<StoreLayoutDetailDto>.Success(StoreLayoutMapper.ToDetailDto(layout));
        }, cancellationToken);
    }
}
