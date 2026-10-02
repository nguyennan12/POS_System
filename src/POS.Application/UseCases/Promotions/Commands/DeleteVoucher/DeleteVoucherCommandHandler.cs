using MediatR;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Promotions.Errors;

namespace POS.Application.UseCases.Promotions.Commands.DeleteVoucher;

public class DeleteVoucherCommandHandler(
    IVoucherRepository voucherRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteVoucherCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteVoucherCommand request, CancellationToken cancellationToken)
    {
        var voucher = await voucherRepository.GetByIdAsync(request.Id, cancellationToken);
        if (voucher is null)
        {
            return PromotionErrors.VoucherNotFound;
        }

        voucherRepository.Remove(voucher);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
