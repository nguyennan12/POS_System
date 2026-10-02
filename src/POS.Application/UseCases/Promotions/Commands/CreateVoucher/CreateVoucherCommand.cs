using MediatR;
using POS.Domain.Common;

namespace POS.Application.UseCases.Promotions.Commands.CreateVoucher;

public record CreateVoucherCommand(
    Guid PromotionId,
    string Code,
    int MaxUses,
    int PerCustomerLimit = 1,
    DateTimeOffset? ExpiresAt = null
) : IRequest<Result<VoucherDto>>;
