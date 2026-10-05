using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Promotions;
using POS.WinUI.Core.Constants;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

/// <summary>
/// Client gọi REST API cho khuyến mãi & voucher (Promotions & Vouchers)
/// </summary>
public sealed class VoucherApiClient : BaseApiClient
{
    public VoucherApiClient(HttpClient http, SessionService session)
        : base(http, session) { }

    /// <summary>
    /// Kiểm tra tính hợp lệ của mã Voucher theo giá trị đơn hàng và khách hàng
    /// </summary>
    public Task<ApiResponse<ValidateVoucherResponse>?> ValidateVoucherAsync(
        string code,
        decimal? orderAmount = null,
        Guid? customerId = null,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>();

        if (orderAmount.HasValue)
            queryParams.Add($"orderAmount={orderAmount.Value}");

        if (customerId.HasValue)
            queryParams.Add($"customerId={customerId.Value}");

        var queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : string.Empty;
        var url = $"{ApiRoutes.Vouchers.Validate(code)}{queryString}";

        return GetAsync<ApiResponse<ValidateVoucherResponse>>(url, ct);
    }
}
