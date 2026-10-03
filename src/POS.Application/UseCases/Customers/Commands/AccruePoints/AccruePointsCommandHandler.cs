using MediatR;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Customers;
using POS.Domain.Customers.Enums;
using POS.Domain.Customers.Errors;

namespace POS.Application.UseCases.Customers.Commands.AccruePoints;

/// <summary>
/// Initializes the AccruePoints handler with customer persistence and a unit of work.
/// </summary>
public class AccruePointsCommandHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AccruePointsCommand, Result<LoyaltyAccountDto>>
{
    /// <summary>
    /// Credits an active customer, creates a loyalty account if needed, and saves the earn transaction and updated balance.
    /// </summary>
    public async Task<Result<LoyaltyAccountDto>> Handle(AccruePointsCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetEntityByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return CustomerErrors.NotFound;
        }

        if (!customer.IsActive)
        {
            return CustomerErrors.Inactive;
        }

        var loyaltyAccount = await customerRepository.GetLoyaltyAccountWithTierAsync(request.CustomerId, cancellationToken);
        if (loyaltyAccount is null)
        {
            loyaltyAccount = new LoyaltyAccount(request.CustomerId, 0);
            await customerRepository.AddLoyaltyAccountAsync(loyaltyAccount, cancellationToken);
        }

        loyaltyAccount.AddPoints(request.Points);

        var pointTx = new PointTransaction(
            customerId: request.CustomerId,
            points: request.Points,
            type: PointTransactionType.Earn,
            orderId: request.OrderId,
            note: request.Note
        );

        await customerRepository.AddPointTransactionAsync(pointTx, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var tier = loyaltyAccount.Customer?.MemberTier ?? customer.MemberTier;
        var rate = tier?.PointRedemptionRate ?? 1000m;
        var balanceInCurrency = tier != null
            ? tier.ConvertPointsToCurrency(loyaltyAccount.PointsBalance)
            : loyaltyAccount.PointsBalance * rate;

        return new LoyaltyAccountDto(
            loyaltyAccount.CustomerId,
            loyaltyAccount.PointsBalance,
            tier?.Name.ToString() ?? "Normal",
            tier?.PointRate ?? 0,
            tier?.DiscountRate ?? 0,
            loyaltyAccount.LastUpdated,
            rate,
            balanceInCurrency
        );
    }
}
