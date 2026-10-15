using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Inventory;

namespace POS.Application.UseCases.Inventory.StockTake.Queries.GetStockTakeById;

public record GetStockTakeByIdQuery(Guid Id) : IQuery<StockTakeDetailDto>;
