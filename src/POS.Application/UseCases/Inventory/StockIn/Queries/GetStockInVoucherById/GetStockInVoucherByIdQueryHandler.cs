using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Inventory;
using POS.Application.Abstractions.Auth;
using POS.Domain.Common;

namespace POS.Application.UseCases.Inventory.StockIn.Queries.GetStockInVoucherById;

public class GetStockInVoucherByIdQueryHandler(
    IStockInVoucherRepository stockInVoucherRepository,
    ICurrentUser currentUser) : IQueryHandler<GetStockInVoucherByIdQuery, StockInVoucherDetailDto>
{
    public async Task<Result<StockInVoucherDetailDto>> Handle(
        GetStockInVoucherByIdQuery query,
        CancellationToken cancellationToken)
    {
        var voucher = await stockInVoucherRepository.GetByIdWithItemsAsync(query.VoucherId, cancellationToken);
        if (voucher is null)
            return StockInErrors.VoucherNotFound;

        if (!currentUser.IsChainOwner && currentUser.StoreId != voucher.StoreId)
            return StockInErrors.AccessDenied;

        return Result<StockInVoucherDetailDto>.Success(voucher.ToDetailDto());
    }
}
