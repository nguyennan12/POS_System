using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Products.Queries.GetSkuById;

public record GetSkuByIdQuery(Guid Id) : IQuery<SkuDetailDto>;
