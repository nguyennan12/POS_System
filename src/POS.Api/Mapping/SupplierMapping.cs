using POS.Application.UseCases.Suppliers;
using POS.Application.UseCases.Suppliers.Queries.GetSuppliers;
using POS.Contracts.V1.Inventory;

namespace POS.Api.Mappings;

public static class SupplierMapping
{
    public static SupplierResponse ToResponse(this SupplierDto dto) =>
        new(dto.Id,
            dto.Name,
            dto.TaxCode,
            dto.ContactName,
            dto.Phone,
            dto.Email,
            dto.Address,
            dto.CreditTerms,
            dto.IsActive,
            dto.CreatedAt);

    public static SupplierPaymentResponse ToResponse(this SupplierPaymentDto dto) =>
        new(dto.Id,
            dto.SupplierId,
            dto.VoucherId,
            dto.Amount,
            dto.Method,
            dto.Note,
            dto.CreatedBy,
            dto.PaidAt);

    public static PagedSupplierList ToPagedList(this GetSuppliersQuery query,
        List<SupplierDto> items, int totalCount) =>
        new(items, totalCount, query.PageNumber, query.PageSize);
}
