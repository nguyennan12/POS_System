using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Invoices;
using POS.Contracts.V1.Orders;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Orders;

namespace POS.WinUI.Tests.Invoices;

public sealed class InvoiceScreenTests
{
    private static readonly Guid Id = Guid.NewGuid(), Store = Guid.NewGuid(), Order = Guid.NewGuid();
    private static readonly DateTimeOffset IssuedAt = new(2026, 11, 2, 12, 30, 0, TimeSpan.FromHours(7));
    private static InvoiceSummaryResponse Summary(Guid? id = null) => new(id ?? Id, Order, "HD-TEST", "Buyer", 200, 18, 198, IssuedAt) { CustomerPhone = "0901234567" };
    private static InvoiceDetailResponse Detail(Guid id) => new(id, Order, "HD-TEST", "Buyer", null, null, 200, 18, 198, IssuedAt,
        [new(Guid.NewGuid(), "Product", "SKU", 2, 100, 198)]);
    private static ReceiptDataResponse Receipt(Guid id) => new("Actual store", "Address", null, "ORDER", "Actual cashier", IssuedAt,
        [new(Guid.NewGuid(), Guid.NewGuid(), "SKU", "Product", 2, 100, 20, 18, 198)], 200, 20, 18, 198, 198, 0, null, null)
    { InvoiceId = id, InvoiceNo = "HD-TEST", IsReprint = true, IssuedAt = IssuedAt, PaperWidth = 80, Payments = [new(Guid.NewGuid(), "Cash", 198, 0, null, "Success", IssuedAt)] };
    private static SessionService Session(bool allowed = true)
    {
        var session = new SessionService();
        session.SetSession("token", "refresh", Guid.NewGuid().ToString(), "Cashier", "Cashier", Store.ToString(),
            permissions: allowed ? ["invoices:read"] : []);
        return session;
    }
    private static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(ApiResponse<T>.Ok(value)) };
    private static HttpClient Http(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) => new(new Handler(respond)) { BaseAddress = new("http://localhost/") };
    private static Task<HttpResponseMessage> DefaultResponse(HttpRequestMessage request, Guid? id = null)
    {
        var invoiceId = id ?? Id;
        var path = request.RequestUri!.AbsolutePath;
        return Task.FromResult(path.EndsWith("/receipt") ? Json(Receipt(invoiceId)) : path == "/api/v1/invoices"
            ? Json(new PagedResponse<InvoiceSummaryResponse>([Summary(invoiceId)], 1, 8, 1)) : Json(Detail(invoiceId)));
    }

    [Fact]
    public async Task Client_encodes_filters_and_authenticates_receipt_and_pdf_download()
    {
        var urls = new List<Uri>();
        using var http = Http((request, _) =>
        {
            urls.Add(request.RequestUri!);
            Assert.Equal("token", request.Headers.Authorization?.Parameter);
            Assert.Equal(Store.ToString(), Assert.Single(request.Headers.GetValues("X-Store-Id")));
            if (request.RequestUri!.Query.Contains("download=true"))
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent("%PDF-test"u8.ToArray()) };
                response.Content.Headers.ContentType = new("application/pdf");
                return Task.FromResult(response);
            }
            return DefaultResponse(request);
        });
        var api = new InvoiceApiClient(http, Session());
        await api.GetAllAsync(new(From: IssuedAt, To: IssuedAt, PageNumber: 2, Search: " HĐ & 090 ", PaymentMethod: "Cash"));
        Assert.Contains("search=H%C4%90%20%26%20090", urls[0].AbsoluteUri);
        Assert.Contains("paymentMethod=Cash", urls[0].Query);
        Assert.Contains("%2B07%3A00", urls[0].AbsoluteUri);
        await api.GetReprintReceiptAsync(Id, 58);
        Assert.Contains("reprint=true&paperWidth=58", urls[1].Query);
        Assert.Equal("%PDF-test"u8.ToArray(), await api.DownloadReprintPdfAsync(Id));
        Assert.Contains("reprint=true&download=true", urls[2].Query);
    }

    [Fact]
    public async Task Live_view_uses_historical_totals_and_reloads_receipt_before_printing()
    {
        var receipts = 0;
        using var http = Http((request, _) => { if (request.RequestUri!.AbsolutePath.EndsWith("/receipt")) receipts++; return DefaultResponse(request); });
        var output = new Output();
        using var vm = new OrdersViewModel(new InvoiceApiClient(http, Session()), Session(), output);
        await vm.InitializeAsync();
        Assert.False(vm.IsPreview);
        Assert.Equal(198, vm.SelectedDetail!.Items[0].Total); // 2 * 100 must not overwrite the historical line amount.
        Assert.Equal(18, vm.SelectedDetail.Tax);
        Assert.Equal(198, vm.SelectedDetail.GrandTotal);
        Assert.Equal(20, vm.SelectedDetail.Discount);
        Assert.Equal("0901234567", vm.SelectedDetail.BuyerPhone);
        Assert.Equal("Actual cashier", vm.SelectedDetail.CashierName);
        vm.OpenReprintCommand.Execute(null); vm.Copies = 2; vm.PaperWidth = 58;
        await vm.ConfirmOutputCommand.ExecuteAsync(null);
        Assert.Equal(2, receipts);
        Assert.Equal(Id, output.Printed!.InvoiceId);
        Assert.True(output.Printed.IsReprint);
        Assert.Equal(2, output.Copies);
        Assert.False(vm.IsReprintOpen);
    }

    [Fact]
    public async Task Pdf_output_downloads_authenticated_bytes_instead_of_opening_metadata_url()
    {
        using var http = Http((request, _) =>
        {
            if (!request.RequestUri!.AbsolutePath.EndsWith("/pdf")) return DefaultResponse(request);
            Assert.Equal("localhost", request.RequestUri.Host);
            Assert.Contains("reprint=true", request.RequestUri.Query);
            if (!request.RequestUri.Query.Contains("download=true"))
                return Task.FromResult(Json(new InvoicePdfResponse(Id, "HD-TEST", "https://untrusted.example/file.pdf")));
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent("%PDF-copy"u8.ToArray()) };
            response.Content.Headers.ContentType = new("application/pdf"); return Task.FromResult(response);
        });
        var session = Session(); var output = new Output();
        using var vm = new OrdersViewModel(new InvoiceApiClient(http, session), session, output);
        await vm.InitializeAsync(); vm.PreviewPdfCommand.Execute(null);
        await vm.ConfirmOutputCommand.ExecuteAsync(null);
        Assert.Equal("%PDF-copy"u8.ToArray(), output.Saved);
    }

    [Fact]
    public async Task Api_failure_remains_visible_and_never_falls_back_to_demo_data()
    {
        using var http = Http((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        { Content = new StringContent("{\"error\":{\"message\":\"Database unavailable\"}}") }));
        var session = Session();
        using var vm = new OrdersViewModel(new InvoiceApiClient(http, session), session, new Output());
        await vm.InitializeAsync();
        Assert.Empty(vm.Invoices); Assert.Equal("Database unavailable", vm.ListError);
        Assert.False(vm.IsPreview); Assert.False(vm.OpenReprintCommand.CanExecute(null));
    }

    [Fact]
    public async Task Closing_view_before_DI_disposal_can_dispose_the_same_view_model_twice()
    {
        using var http = Http((request, _) => DefaultResponse(request));
        var session = Session();
        var vm = new OrdersViewModel(new InvoiceApiClient(http, session), session, new Output());
        await vm.InitializeAsync();
        vm.Dispose();
        Assert.Null(Record.Exception(vm.Dispose));
    }

    [Fact]
    public async Task Missing_permission_does_not_query_invoices()
    {
        using var http = Http((_, _) => throw new Exception("No request expected"));
        var session = Session(false);
        using var vm = new OrdersViewModel(new InvoiceApiClient(http, session), session, new Output());
        await vm.InitializeAsync(); Assert.Contains("invoices:read", vm.ListError);
    }

    [Fact]
    public async Task Selection_change_ignores_an_old_detail_even_if_transport_ignores_cancellation()
    {
        var second = Guid.NewGuid();
        var delayedDetail = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var http = Http((request, _) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/api/v1/invoices") return Task.FromResult(Json(new PagedResponse<InvoiceSummaryResponse>([Summary(), Summary(second)], 1, 8, 2)));
            if (path == $"/api/v1/invoices/{Id}") return delayedDetail.Task;
            return DefaultResponse(request, path.Contains(second.ToString()) ? second : Id);
        });
        var session = Session();
        using var vm = new OrdersViewModel(new InvoiceApiClient(http, session), session, new Output());
        await vm.InitializeAsync();
        vm.SelectedInvoice = vm.Invoices[1];
        Assert.Equal(second, vm.SelectedDetail!.Id);
        delayedDetail.SetResult(Json(Detail(Id)));
        await Task.Delay(50);
        Assert.Equal(second, vm.SelectedDetail.Id); Assert.False(vm.IsDetailLoading);
    }

    private sealed class Output : IInvoiceOutputService
    {
        public ReceiptDataResponse? Printed; public int Copies; public byte[]? Saved;
        public IReadOnlyList<string> GetPrinterNames() => ["Test printer"];
        public bool PrintReceipt(ReceiptDataResponse receipt, string printerName, int copies)
        { Printed = receipt; Copies = copies; return true; }
        public bool SavePdf(byte[] bytes, string invoiceNo) { Saved = bytes; return true; }
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request, cancellationToken); }
}
