using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Common;

namespace POS.Application.UseCases.Suppliers.Queries.GetSuppliers;

public class GetSuppliersQueryHandler : IQueryHandler<GetSuppliersQuery, PagedSupplierList>
{
    private readonly ISupplierRepository _supplierRepository;

    public GetSuppliersQueryHandler(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<Result<PagedSupplierList>> Handle(
        GetSuppliersQuery query,
        CancellationToken cancellationToken)
    {
        var (items, total) = await _supplierRepository.GetPagedAsync(
            query.Search,
            query.IsActive,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var dtos = items.Select(s => s.ToDto()).ToList();

        return new PagedSupplierList(dtos, total, query.PageNumber, query.PageSize);
    }
}
