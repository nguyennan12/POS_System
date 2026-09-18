using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Customers;

namespace POS.Infrastructure.Persistence.Repositories;

public class CustomerRepository(AppDbContext context) : ICustomerRepository
{
    public async Task<CustomerWithPoints?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = from c in context.Customers.Include(c => c.MemberTier)
                    join l in context.LoyaltyAccounts on c.Id equals l.CustomerId into lGroup
                    from l in lGroup.DefaultIfEmpty()
                    where c.Id == id
                    select new CustomerWithPoints(c, l != null ? l.PointsBalance : 0);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Customer?> GetEntityByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Customers
            .Include(c => c.MemberTier)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<CustomerWithPoints?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        var query = from c in context.Customers.Include(c => c.MemberTier)
                    join l in context.LoyaltyAccounts on c.Id equals l.CustomerId into lGroup
                    from l in lGroup.DefaultIfEmpty()
                    where c.Phone == phone
                    select new CustomerWithPoints(c, l != null ? l.PointsBalance : 0);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CustomerWithPoints?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var query = from c in context.Customers.Include(c => c.MemberTier)
                    join l in context.LoyaltyAccounts on c.Id equals l.CustomerId into lGroup
                    from l in lGroup.DefaultIfEmpty()
                    where c.Barcode == barcode
                    select new CustomerWithPoints(c, l != null ? l.PointsBalance : 0);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> IsPhoneUniqueAsync(string phone, Guid? excludeCustomerId = null, CancellationToken cancellationToken = default) =>
        context.Customers
            .AnyAsync(c => c.Phone == phone && (!excludeCustomerId.HasValue || c.Id != excludeCustomerId.Value), cancellationToken)
            .ContinueWith(t => !t.Result, cancellationToken);

    public Task<bool> IsBarcodeUniqueAsync(string barcode, Guid? excludeCustomerId = null, CancellationToken cancellationToken = default) =>
        context.Customers
            .AnyAsync(c => c.Barcode == barcode && (!excludeCustomerId.HasValue || c.Id != excludeCustomerId.Value), cancellationToken)
            .ContinueWith(t => !t.Result, cancellationToken);

    public async Task<(List<CustomerWithPoints> Items, int TotalCount)> GetPagedAsync(
        string? phone,
        string? name,
        string? barcode,
        Guid? memberTierId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = context.Customers.AsNoTracking().Include(c => c.MemberTier).AsQueryable();

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var p = phone.Trim();
            baseQuery = baseQuery.Where(c => c.Phone.Contains(p));
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            var n = name.Trim();
            baseQuery = baseQuery.Where(c => EF.Functions.ILike(c.Name, $"%{n}%"));
        }

        if (!string.IsNullOrWhiteSpace(barcode))
        {
            var b = barcode.Trim();
            baseQuery = baseQuery.Where(c => c.Barcode != null && c.Barcode.Contains(b));
        }

        if (memberTierId.HasValue)
        {
            baseQuery = baseQuery.Where(c => c.MemberTierId == memberTierId.Value);
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var pNum = pageNumber < 1 ? 1 : pageNumber;
        var pSize = pageSize < 1 ? 20 : pageSize;

        var pagedCustomers = await (from c in baseQuery.OrderByDescending(c => c.CreatedAt)
                                    join l in context.LoyaltyAccounts.AsNoTracking() on c.Id equals l.CustomerId into lGroup
                                    from l in lGroup.DefaultIfEmpty()
                                    select new CustomerWithPoints(c, l != null ? l.PointsBalance : 0))
            .Skip((pNum - 1) * pSize)
            .Take(pSize)
            .ToListAsync(cancellationToken);

        return (pagedCustomers, totalCount);
    }

    public async Task AddAsync(Customer customer, LoyaltyAccount loyaltyAccount, CancellationToken cancellationToken = default)
    {
        await context.Customers.AddAsync(customer, cancellationToken);
        await context.LoyaltyAccounts.AddAsync(loyaltyAccount, cancellationToken);
    }

    public Task<bool> HasOrdersAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        context.Orders.AnyAsync(o => o.CustomerId == customerId, cancellationToken);

    public void Remove(Customer customer) => context.Customers.Remove(customer);
}
