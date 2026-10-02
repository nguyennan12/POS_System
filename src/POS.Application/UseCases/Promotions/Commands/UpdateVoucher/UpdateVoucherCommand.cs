using MediatR;
using POS.Domain.Common;

namespace POS.Application.UseCases.Promotions.Commands.UpdateVoucher;

public record UpdateVoucherCommand(
    Guid Id,
    int MaxUses,
    int PerCustomerLimit,
    DateTimeOffset? ExpiresAt,
    bool IsActive
) : IRequest<Result<VoucherDto>>;
