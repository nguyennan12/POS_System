using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.StoreLayouts;

namespace POS.Application.UseCases.StoreLayouts.Commands.CreateStoreLayout;

public class CreateStoreLayoutCommandHandler : ICommandHandler<CreateStoreLayoutCommand, StoreLayoutDetailDto>
{
    private readonly IStoreLayoutRepository _storeLayoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStoreAccessPolicy _storeAccessPolicy;
    private readonly ICurrentUser _currentUser;

    public CreateStoreLayoutCommandHandler(
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

    public async Task<Result<StoreLayoutDetailDto>> Handle(CreateStoreLayoutCommand request, CancellationToken cancellationToken)
    {
        var storeResult = _storeAccessPolicy.EnforceStoreAccess(request.StoreId);
        if (storeResult.IsFailure) return Result<StoreLayoutDetailDto>.Failure(storeResult.Error);
        var storeId = storeResult.Value;

        var nextVersion = await _storeLayoutRepository.GetNextVersionNumberAsync(storeId, cancellationToken);
        var actorId = _currentUser.EmployeeId ?? Guid.Empty;

        var createResult = StoreLayout.Create(
            storeId,
            nextVersion,
            request.Name,
            request.GridWidth,
            request.GridHeight,
            actorId,
            DateTime.UtcNow);

        if (createResult.IsFailure)
            return Result<StoreLayoutDetailDto>.Failure(createResult.Error);

        var layout = createResult.Value;
        _storeLayoutRepository.Add(layout);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<StoreLayoutDetailDto>.Success(StoreLayoutMapper.ToDetailDto(layout));
    }
}
