using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Contracts.V1.Registers;
using POS.Domain.Common;

namespace POS.Application.UseCases.Registers.Queries.GetRegistersByStore;

public class GetRegistersByStoreQueryHandler(IPosRegisterRepository registerRepository)
    : IQueryHandler<GetRegistersByStoreQuery, IReadOnlyList<RegisterResponse>>
{
    public async Task<Result<IReadOnlyList<RegisterResponse>>> Handle(
        GetRegistersByStoreQuery query,
        CancellationToken cancellationToken)
    {
        var registers = await registerRepository.GetByStoreIdAsync(query.StoreId, cancellationToken);
        var response = registers.Select(r => new RegisterResponse(
            r.Id,
            r.StoreId,
            r.Name,
            r.Code,
            r.IsActive,
            r.CreatedAt
        )).ToList();

        return Result<IReadOnlyList<RegisterResponse>>.Success(response);
    }
}
