using MediatR;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;
using POS.Domain.Customers;
using POS.Domain.Customers.Enums;
using POS.Domain.Customers.Errors;

namespace POS.Application.UseCases.Customers.Commands.RedeemPoints;

///  
/// Initializes the RedeemPoints handler with customer persistence and a unit of work.
/// </summary>
public class RedeemPointsCommandHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RedeemPointsCommand, Result<LoyaltyAccountDto>>
{

  /// Debits an active customer's existing loyalty account when sufficient points are available and saves the redemption transaction.
  /// </summary>
  public async Task<Result<LoyaltyAccountDto>> Handle(RedeemPointsCommand request, CancellationToken cancellationToken)
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
          return CustomerErrors.LoyaltyAccountNotFound;
        }

        if (loyaltyAccount.PointsBalance < request.Points)
        {
          return CustomerErrors.InsufficientPoints;
        }

        loyaltyAccount.DeductPoints(request.Points);

        var pointTx = new PointTransaction(
                  customerId: request.CustomerId,
                  points: request.Points,
                  type: PointTransactionType.Redeem,
                  orderId: request.OrderId,
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
