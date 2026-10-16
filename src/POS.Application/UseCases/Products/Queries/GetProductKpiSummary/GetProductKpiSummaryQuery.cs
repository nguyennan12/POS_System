using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Products.Queries.GetProductKpiSummary;

public record GetProductKpiSummaryQuery(Guid? StoreId = null) : IQuery<ProductKpiSummaryDto>;
