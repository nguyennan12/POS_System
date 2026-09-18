using POS.Domain.Customers;

namespace POS.Application.Abstractions.Persistence;

public record CustomerWithPoints(Customer Customer, decimal PointsBalance);

public interface ICustomerRepository
{
    Task<CustomerWithPoints?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
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
    Task<bool> HasOrdersAsync(Guid customerId, CancellationToken cancellationToken = default);
    void Remove(Customer customer);
}
