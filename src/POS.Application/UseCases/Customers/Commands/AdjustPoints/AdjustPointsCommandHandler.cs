using MediatR;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Customers;
using POS.Domain.Customers.Enums;
using POS.Domain.Customers.Errors;

namespace POS.Application.UseCases.Customers.Commands.AdjustPoints;

///  
/// Initializes the AdjustPoints handler with customer persistence and a unit of work.
/// </summary>
public class AdjustPointsCommandHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AdjustPointsCommand, Result<LoyaltyAccountDto>>
{

  /// Applies a signed adjustment for an active customer, rejects insufficient balances, and saves the adjustment transaction.
  /// </summary>
  public async Task<Result<LoyaltyAccountDto>> Handle(AdjustPointsCommand request, CancellationToken cancellationToken)
  {
    // Replay the complete decision after a serialization conflict, as checkout does.
    // UnitOfWork rolls back and clears tracked entities before the next attempt.
    for (var attempt = 0; ; attempt++)
    {
      var result = await unitOfWork.ExecuteSerializableAsync<LoyaltyAccountDto>(async ct =>
      {
        var customer = await customerRepository.GetEntityByIdAsync(request.CustomerId, ct);
        if (customer is null)
        {
          return CustomerErrors.NotFound;
        }

        if (!customer.IsActive)
        {
          return CustomerErrors.Inactive;
        }

        var loyaltyAccount = await customerRepository.GetLoyaltyAccountWithTierAsync(request.CustomerId, ct);
        if (loyaltyAccount is null)
        {
          if (request.Points < 0)
          {
            return CustomerErrors.InsufficientPoints;
          }

          loyaltyAccount = new LoyaltyAccount(request.CustomerId, 0);
          await customerRepository.AddLoyaltyAccountAsync(loyaltyAccount, ct);
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

        await customerRepository.AddPointTransactionAsync(pointTx, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var tier = loyaltyAccount.Customer?.MemberTier ?? customer.MemberTier;

        return new LoyaltyAccountDto(
                  loyaltyAccount.CustomerId,
                  loyaltyAccount.PointsBalance,
                  tier?.Name.ToString() ?? "Normal",
                  tier?.PointRate ?? 0,
                  tier?.DiscountRate ?? 0,
                  loyaltyAccount.LastUpdated
              );
      }, cancellationToken);
      if (attempt < 2 && result.IsFailure && result.Error.Code == "Persistence.ConcurrentModification")
        continue;
      return result;
    }
  }
}
