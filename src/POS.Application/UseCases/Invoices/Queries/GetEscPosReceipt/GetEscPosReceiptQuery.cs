using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;

namespace POS.Application.UseCases.Invoices.Queries.GetEscPosReceipt;

public sealed record GetEscPosReceiptQuery(Guid Id, int? PaperWidth = null, bool Reprint = false)
    : IQuery<byte[]>, IRequirePermission
{
    public string RequiredPermission => "invoices:read";
}
