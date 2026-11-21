using POS.Domain.Common;
using POS.Domain.StoreLayouts;

namespace POS.Application.UseCases.StoreLayouts.Services;

public interface IStoreLayoutValidationService
{
    Task<Result> ValidateForPublishAsync(
        StoreLayout layout,
        Guid expectedStoreId,
        CancellationToken cancellationToken = default);
}
