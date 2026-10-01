using POS.Domain.Customers;
using POS.Domain.Customers.Enums;

namespace POS.Application.Abstractions.Persistence;

public record CustomerWithPoints(Customer Customer, decimal PointsBalance);

public interface ICustomerRepository
{
    Task<CustomerWithPoints?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LoyaltyAccount?> GetLoyaltyAccountAsync(Guid customerId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Loads the customer's loyalty account with its customer and membership tier, or returns null if absent.
    /// </summary>
    Task<LoyaltyAccount?> GetLoyaltyAccountWithTierAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<Customer?> GetEntityByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerWithPoints?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<CustomerWithPoints?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<bool> IsPhoneUniqueAsync(string phone, Guid? excludeCustomerId = null, CancellationToken cancellationToken = default);
    Task<bool> IsBarcodeUniqueAsync(string barcode, Guid? excludeCustomerId = null, CancellationToken cancellationToken = default);
    Task<(List<CustomerWithPoints> Items, int TotalCount)> GetPagedAsync(
        string? phone,
        string? name,
        string? barcode,
        Guid? memberTierId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, LoyaltyAccount loyaltyAccount, CancellationToken cancellationToken = default);
    Task AddLoyaltyAccountAsync(LoyaltyAccount loyaltyAccount, CancellationToken cancellationToken = default);
    /// <summary>
    /// Adds a point transaction to the unit of work for persistence when changes are saved.
    /// </summary>
    Task AddPointTransactionAsync(PointTransaction transaction, CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns a page of customer transactions ordered newest first and the total matching count, with optional inclusive date and type filters.
    /// </summary>
    Task<(List<PointTransaction> Items, int TotalCount)> GetPointTransactionsPagedAsync(
        Guid customerId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        PointTransactionType? type,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<bool> HasOrdersAsync(Guid customerId, CancellationToken cancellationToken = default);
    void Remove(Customer customer);
}
