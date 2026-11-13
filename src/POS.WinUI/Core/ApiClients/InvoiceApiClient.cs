using System.Globalization;
using System.Net.Http;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Invoices;
using POS.Contracts.V1.Orders;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

public sealed class InvoiceApiClient(HttpClient http, SessionService session) : BaseApiClient(http, session)
{
    private const string Route = "api/v1/invoices";

    public async Task<PagedResponse<InvoiceSummaryResponse>> GetAllAsync(InvoiceFilterRequest filter, CancellationToken ct = default)
    {
        var query = $"?pageNumber={filter.PageNumber}&pageSize={filter.PageSize}";
        if (filter.StoreId.HasValue && filter.StoreId.Value != Guid.Empty) query += $"&storeId={filter.StoreId.Value}";
        if (filter.OrderId.HasValue) query += $"&orderId={filter.OrderId}";
        if (filter.From.HasValue) query += "&from=" + EncodeDate(filter.From.Value);
        if (filter.To.HasValue) query += "&to=" + EncodeDate(filter.To.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search)) query += "&search=" + Uri.EscapeDataString(filter.Search.Trim());
        if (!string.IsNullOrWhiteSpace(filter.PaymentMethod)) query += "&paymentMethod=" + Uri.EscapeDataString(filter.PaymentMethod);
        return RequireData(await GetAsync<ApiResponse<PagedResponse<InvoiceSummaryResponse>>>(Route + query, ct));
    }

    public async Task<InvoiceDetailResponse> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        RequireData(await GetAsync<ApiResponse<InvoiceDetailResponse>>($"{Route}/{id}", ct));

    public async Task<ReceiptDataResponse> GetReprintReceiptAsync(Guid id, int paperWidth, CancellationToken ct = default) =>
        RequireData(await GetAsync<ApiResponse<ReceiptDataResponse>>($"{Route}/{id}/receipt?reprint=true&paperWidth={paperWidth}", ct));

    public async Task<InvoicePdfResponse> GetReprintPdfAsync(Guid id, CancellationToken ct = default) =>
        RequireData(await GetAsync<ApiResponse<InvoicePdfResponse>>($"{Route}/{id}/pdf?reprint=true", ct));

    public async Task<byte[]> DownloadReprintPdfAsync(Guid id, CancellationToken ct = default)
    {
        AttachToken();
        // Download through the authenticated API client, never launch an unauthenticated PdfUrl.
        using var response = await _http.GetAsync($"{Route}/{id}/pdf?reprint=true&download=true", ct);
        await HandleFailureResponseAsync(response, ct);
        if (response.Content.Headers.ContentType?.MediaType != "application/pdf")
            throw new InvalidOperationException("API không trả về file PDF hợp lệ.");
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        if (bytes.Length < 5 || !bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
            throw new InvalidOperationException("Nội dung PDF không hợp lệ.");
        return bytes;
    }

    private static string EncodeDate(DateTimeOffset date) => Uri.EscapeDataString(date.ToString("O", CultureInfo.InvariantCulture));
    private static T RequireData<T>(ApiResponse<T>? response) => response is { Success: true, Data: not null }
        ? response.Data : throw new InvalidOperationException(response?.Error?.Message ?? "API hóa đơn không trả về dữ liệu.");
}
