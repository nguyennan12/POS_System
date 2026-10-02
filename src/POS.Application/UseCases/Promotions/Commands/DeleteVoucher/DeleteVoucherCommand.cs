using MediatR;
using POS.Domain.Common;

namespace POS.Application.UseCases.Promotions.Commands.DeleteVoucher;

public record DeleteVoucherCommand(Guid Id) : IRequest<Result<bool>>;
