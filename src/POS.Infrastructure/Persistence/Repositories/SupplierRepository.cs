using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Inventory.Suppliers;

namespace POS.Infrastructure.Persistence.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly AppDbContext _context;

    public SupplierRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<(List<Supplier> Items, int TotalCount)> GetPagedAsync(
        string? search,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Suppliers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(term) ||
                (s.TaxCode != null && s.TaxCode.ToLower().Contains(term)) ||
                (s.Phone != null && s.Phone.Contains(term)) ||
                (s.ContactName != null && s.ContactName.ToLower().Contains(term)));
        }

        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default) =>
        await _context.Suppliers.AddAsync(supplier, cancellationToken);

    public Task<List<SupplierPayment>> GetPaymentsBySupplierAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default) =>
        _context.SupplierPayments
            .AsNoTracking()
            .Where(p => p.SupplierId == supplierId)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync(cancellationToken);

    public async Task AddPaymentAsync(SupplierPayment payment, CancellationToken cancellationToken = default) =>
        await _context.SupplierPayments.AddAsync(payment, cancellationToken);
}
