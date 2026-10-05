using POS.Domain.Inventory.Stock;

namespace POS.Application.Abstractions.Persistence;

public interface IStockBatchRepository
{
  ///  Lấy lô hàng theo storeId + skuId + batchNo (unique key).</summary>
  Task<StockBatch?> GetByBatchNoAsync(Guid storeId, Guid skuId, string batchNo,
      CancellationToken cancellationToken = default);

  Task<StockBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

  Task AddAsync(StockBatch batch, CancellationToken cancellationToken = default);
}
