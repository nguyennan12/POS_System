using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Employees;
using POS.Domain.Employees.Enums;
using POS.Domain.Orders.Enums;

namespace POS.Infrastructure.Persistence.Repositories;

public class ShiftRepository(AppDbContext context) : IShiftRepository
{
    public async Task<Shift?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Shifts
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Shift?> GetOpenShiftAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        return await context.Shifts
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(
                s => s.StoreId == storeId && s.Status == ShiftStatus.Open,
                cancellationToken);
    }

    public async Task AddAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        await context.Shifts.AddAsync(shift, cancellationToken);
    }

    public async Task<ShiftSalesData> GetShiftSalesAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        // Aggregate payments from paid orders in this shift
        var data = await (
            from o in context.Orders
            where o.ShiftId == shiftId
                  && o.Status == OrderStatus.Paid
            join p in context.Payments on o.Id equals p.OrderId
            where p.Status == PaymentStatus.Success
            group p by 1 into g
            select new
            {
                CashSales = g.Where(p => p.Method == PaymentMethod.Cash)
                             .Sum(p => p.Amount),
                CardSales = g.Where(p => p.Method == PaymentMethod.Card)
                             .Sum(p => p.Amount),
                QrSales = g.Where(p => p.Method == PaymentMethod.MoMo
                                       || p.Method == PaymentMethod.VietQR)
                            .Sum(p => p.Amount),
                OrderCount = g.Select(p => p.OrderId).Distinct().Count()
            }
        ).FirstOrDefaultAsync(cancellationToken);

        return new ShiftSalesData(
            CashSales: data?.CashSales ?? 0,
            CardSales: data?.CardSales ?? 0,
            QrSales: data?.QrSales ?? 0,
            Refunds: 0, // Refunds not yet implemented
            OrderCount: data?.OrderCount ?? 0);
    }
}
