using MediatR;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Customers;
using POS.Domain.Customers.Enums;
using POS.Domain.Customers.Errors;

namespace POS.Application.UseCases.Customers.Commands.AdjustPoints;

public class AdjustPointsCommandHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AdjustPointsCommand, Result<LoyaltyAccountDto>>
{
    public async Task<Result<LoyaltyAccountDto>> Handle(AdjustPointsCommand request, CancellationToken cancellationToken)
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
            if (request.Points < 0)
            {
                return CustomerErrors.InsufficientPoints;
            }

            loyaltyAccount = new LoyaltyAccount(request.CustomerId, 0);
            await customerRepository.AddAsync(customer, loyaltyAccount, cancellationToken);
        }

        if (loyaltyAccount.PointsBalance + request.Points < 0)
        {
            return CustomerErrors.InsufficientPoints;
        }

        if (request.Points > 0)
        {
            loyaltyAccount.AddPoints(request.Points);
        }
        else
        {
            loyaltyAccount.DeductPoints(-request.Points);
        }

        var pointTx = new PointTransaction(
            customerId: request.CustomerId,
            points: request.Points,
            type: PointTransactionType.Adjust,
            orderId: null,
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
