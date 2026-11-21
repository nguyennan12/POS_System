using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.StoreLayouts.Commands.ArchiveStoreLayout;

public class ArchiveStoreLayoutCommandHandler : ICommandHandler<ArchiveStoreLayoutCommand, StoreLayoutDetailDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;
    private readonly ICurrentUser _currentUser;

    public ArchiveStoreLayoutCommandHandler(
        IStoreLayoutRepository storeLayoutRepository,
        IUnitOfWork unitOfWork,
        IStoreAccessPolicy storeAccessPolicy,
        ICurrentUser currentUser)
    {
        _storeLayoutRepository = storeLayoutRepository;
        _unitOfWork = unitOfWork;
        _storeAccessPolicy = storeAccessPolicy;
        _currentUser = currentUser;
    }

    public async Task<Result<StoreLayoutDetailDto>> Handle(ArchiveStoreLayoutCommand request, CancellationToken cancellationToken)
    {
        var layout = await _storeLayoutRepository.GetByIdAsync(request.LayoutId, includeGraph: true, cancellationToken);
        if (layout == null)
            return Result<StoreLayoutDetailDto>.Failure(StoreLayoutErrors.NotFound);

        if (!_storeAccessPolicy.CanAccessStore(layout.StoreId))
            return Result<StoreLayoutDetailDto>.Failure(StoreLayoutErrors.StoreMismatch);

        if (layout.Revision != request.ExpectedRevision)
            return Result<StoreLayoutDetailDto>.Failure(StoreLayoutErrors.RevisionConflict);

        var actorId = _currentUser.EmployeeId ?? Guid.Empty;
        var archiveResult = layout.Archive(actorId, DateTime.UtcNow);
        if (archiveResult.IsFailure)
            return Result<StoreLayoutDetailDto>.Failure(archiveResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<StoreLayoutDetailDto>.Success(StoreLayoutMapper.ToDetailDto(layout));
    }
}
