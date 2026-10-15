using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Domain.Common;
using POS.Domain.Inventory.Enums;

namespace POS.Application.UseCases.Inventory.StockTake.Commands.SubmitStockTake;

public class SubmitStockTakeCommandHandler(
    IStockTakeRepository stockTakeRepository,
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : ICommandHandler<SubmitStockTakeCommand, StockTakeDetailDto>
{
    public async Task<Result<StockTakeDetailDto>> Handle(
        SubmitStockTakeCommand command,
        CancellationToken cancellationToken)
    {
        if (currentUser.EmployeeId is null)
            return StockTakeErrors.Unauthorized;

        var employee = await employeeRepository.GetByIdAsync(currentUser.EmployeeId.Value, cancellationToken);
        if (employee is null || !employee.IsActive)
            return StockTakeErrors.Unauthorized;

        var stockTake = await stockTakeRepository.GetByIdWithItemsAsync(command.StockTakeId, cancellationToken);
        if (stockTake is null)
            return StockTakeErrors.NotFound;

        if (!employee.IsChainOwner && stockTake.StoreId != employee.StoreId)
            return StockTakeErrors.AccessDenied;

        if (stockTake.Status == StockTakeStatus.Approved)
            return StockTakeErrors.AlreadyApproved;

        if (!stockTake.Items.Any())
            return StockTakeErrors.NoItems;

        var submitResult = stockTake.Submit();
        if (submitResult.IsFailure)
            return submitResult.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await stockTakeRepository.GetByIdWithItemsAsync(stockTake.Id, cancellationToken);
        return Result<StockTakeDetailDto>.Success(updated!.ToDetailDto());
    }
}
