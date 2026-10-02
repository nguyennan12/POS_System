using MediatR;
using POS.Domain.Common;

namespace POS.Application.UseCases.Promotions.Queries.GetPromotionById;

public record GetPromotionByIdQuery(Guid Id) : IRequest<Result<PromotionDetailDto>>;
