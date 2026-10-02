using MediatR;
using POS.Domain.Common;

namespace POS.Application.UseCases.Promotions.Commands.DeletePromotion;

public record DeletePromotionCommand(Guid Id) : IRequest<Result<bool>>;
