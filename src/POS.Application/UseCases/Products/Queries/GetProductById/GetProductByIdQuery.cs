using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IQuery<ProductDetailDto>;
