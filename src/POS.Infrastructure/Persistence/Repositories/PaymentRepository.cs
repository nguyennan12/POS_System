using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Orders;

namespace POS.Infrastructure.Persistence.Repositories;

public class PaymentRepository(AppDbContext dbContext) : IPaymentRepository
{
    public Task<Payment?> GetByIdWithOrderAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Payments.AsNoTracking().Include(payment => payment.Order)
            .FirstOrDefaultAsync(payment => payment.Id == id, cancellationToken);
}
