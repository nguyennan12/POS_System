using POS.Application.UseCases.Customers;
using POS.Contracts.V1.Customers;

namespace POS.Api.Mappings;

public static class CustomerMappings
{
    public static CustomerDetailResponse ToResponse(this CustomerDto dto) =>
        new(
            dto.Id,
            dto.Name,
            dto.Phone,
            dto.Email,
            dto.Dob,
            dto.Barcode,
            dto.MemberTierId,
            dto.MemberTierName,
            dto.TotalSpending,
            dto.PointsBalance,
            dto.IsActive,
            dto.CreatedAt
        );

    public static CustomerSummaryResponse ToSummaryResponse(this CustomerDto dto) =>
        new(
            dto.Id,
            dto.Name,
            dto.Phone,
            dto.Email,
            dto.Barcode,
            dto.MemberTierId,
            dto.MemberTierName,
            dto.TotalSpending,
            dto.PointsBalance,
            dto.IsActive,
            dto.CreatedAt
        );

    public static MemberTierResponse ToResponse(this MemberTierDto dto) =>
        new(
            dto.Id,
            dto.Name,
            dto.MinSpending,
            dto.PointRate,
            dto.DiscountRate,
            dto.DisplayColor,
            dto.PointRedemptionRate
        );

    /// <summary>
    /// Maps a loyalty account and its tier benefits to the API response.
    /// </summary>
    public static LoyaltyAccountResponse ToResponse(this LoyaltyAccountDto dto) =>
        new(
            dto.CustomerId,
            dto.PointsBalance,
            dto.TierName,
            dto.PointRate,
            dto.DiscountRate,
            dto.LastUpdated,
            dto.PointRedemptionRate,
            dto.AvailableBalanceInCurrency
        );

    /// <summary>
    /// Maps a point transaction to the API response, preserving its type and order reference.
    /// </summary>
    public static PointTransactionResponse ToResponse(this PointTransactionDto dto) =>
        new(
            dto.Id,
            dto.CustomerId,
            dto.Points,
            dto.Type,
            dto.OrderId,
            dto.Note,
            dto.CreatedAt
        );
}
