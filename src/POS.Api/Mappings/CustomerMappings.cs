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
            dto.DisplayColor
        );
}
