using MediatR;
using POS.Domain.Common;

namespace POS.Application.UseCases.Promotions.Queries.GetVoucherById;

public record GetVoucherByIdQuery(Guid Id) : IRequest<Result<VoucherDto>>;
