using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.UseCases.Stores.Queries.GetStoreDetail;

namespace POS.Application.UseCases.Stores.Commands.UpdateStore;

public record UpdateStoreCommand(Guid StoreId, string Name, string? Address, string? Phone,
    string Timezone, string CurrencyCode, string? TaxCode, string? ReceiptHeader, string? ReceiptFooter)
    : ICommand<StoreDetailDto>, IRequirePermission
{
    public string RequiredPermission => "stores:update";
}
