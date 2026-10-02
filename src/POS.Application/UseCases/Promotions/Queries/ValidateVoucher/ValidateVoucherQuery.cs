using MediatR;
using POS.Domain.Common;

namespace POS.Application.UseCases.Promotions.Queries.ValidateVoucher;

public record ValidateVoucherQuery(
    string Code,
    decimal OrderSubtotal,
    Guid? CustomerId = null,
    Guid? StoreId = null
) : IRequest<Result<ValidateVoucherResultDto>>;
