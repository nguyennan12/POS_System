using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POS.Contracts.V1.Invoices;
using POS.Contracts.V1.Orders;
using POS.WinUI.Core.ApiClients;
using POS.WinUI.Core.Constants;
using POS.WinUI.Core.Services;
using POS.WinUI.ViewModels.Common;
using Wpf.Ui.Controls;

namespace POS.WinUI.ViewModels.Orders;

public sealed partial class OrdersViewModel : ManagementTabViewModelBase
{
    public override string TabId => "Orders";
    public override string Title => Loc["orders.title"];
    public override string Subtitle => Loc["orders.subtitle"];
    private const int PageSize = 8;
    public override SymbolRegular Icon => SymbolRegular.Receipt24;
    private readonly InvoiceApiClient? _api;
    private readonly IInvoiceOutputService? _output;
    private readonly IReadOnlyList<InvoiceViewItem>? _preview;
    private InvoiceFilterRequest _appliedFilter = new(PageSize: PageSize);
    private CancellationTokenSource? _listCts, _detailCts, _outputCts;
    private int _listVersion, _detailVersion, _contextVersion, _totalCount;
    private bool _initialized, _settingSelection, _disposed;
    public bool IsPreview => _preview != null;
    public bool CanRead => IsPreview || HasPermission(AppPermissions.Invoices.Read);
    public ObservableCollection<InvoiceViewItem> Invoices { get; } = [];
    public ObservableCollection<string> PrinterOptions { get; } = [];
    public string[] PaymentMethods { get; } = ["Tất cả thanh toán", "Tiền mặt", "VietQR", "Thẻ ngân hàng", "MoMo", "Điểm thành viên"];
    public int[] PaperWidths { get; } = [80, 58];
    public int[] CopyOptions { get; } = [1, 2, 3];
    [ObservableProperty] private string _search = "";
    [ObservableProperty] private DateTime? _fromDate;
    [ObservableProperty] private DateTime? _toDate;
    [ObservableProperty] private string _paymentFilter = "Tất cả thanh toán";
    [ObservableProperty] private string? _filterError;
    [ObservableProperty] private string? _listError;
    [ObservableProperty] private string? _detailError;
    [ObservableProperty] private string? _outputError;
    [ObservableProperty] private InvoiceViewItem? _selectedInvoice;
    [ObservableProperty] private InvoiceViewItem? _selectedDetail;
    [ObservableProperty] private InvoiceViewItem? _outputInvoice;
    [ObservableProperty] private bool _isDetailLoading;
    [ObservableProperty] private bool _isReprintOpen;
    [ObservableProperty] private bool _isOutputBusy;
    [ObservableProperty] private int _paperWidth = 80;
    [ObservableProperty] private int _copies = 1;
    [ObservableProperty] private string _selectedPrinter = InvoiceOutputService.ChoosePrinter;
    [ObservableProperty] private bool _isPdfPreview;
    [ObservableProperty] private int _pageNumber = 1;
    public bool HasSelection => SelectedDetail != null;
    public bool ShowDetailPlaceholder => !HasSelection && !IsDetailLoading && DetailError == null;
    public bool IsEmpty => !IsLoading && ListError == null && Invoices.Count == 0;
    public string ResultCountText => $"{_totalCount} hóa đơn";
    public string PageText => $"Trang {PageNumber} / {Math.Max(1, (int)Math.Ceiling(_totalCount / (double)PageSize))}";
    public string RangeText => _totalCount == 0 ? "Không có kết quả" : $"Hiển thị {(PageNumber - 1) * PageSize + 1}–{(PageNumber - 1) * PageSize + Invoices.Count} / {_totalCount} hóa đơn";
    public bool HasInvoices => Invoices.Count > 0;
    public string TotalCountText => _totalCount.ToString("N0");
    public string PageTotalText => InvoiceViewItem.Money(Invoices.Sum(x => x.GrandTotal));
    public string VisibleCountText => Invoices.Count.ToString("N0");
    public string ModeLabel => IsPreview ? "FRONTEND PREVIEW" : "HÓA ĐƠN BÁN LẺ";
    public string BannerText => IsPreview
        ? "Dữ liệu minh họa · Chưa kết nối API / database. Thao tác in và xuất PDF chỉ được mô phỏng."
        : IsOwner ? "Dữ liệu trực tiếp từ hệ thống · Chủ chuỗi có thể tra cứu hóa đơn trên toàn chuỗi."
        : "Dữ liệu trực tiếp từ hệ thống · Tra cứu hóa đơn thuộc cửa hàng của bạn.";
    public string DialogTitle => IsPdfPreview ? "Xuất PDF bản sao" : "In lại hóa đơn";
    public string DialogActionText => IsOutputBusy ? "Đang xử lý…" : IsPdfPreview
        ? (IsPreview ? "Mô phỏng xuất PDF" : "Lưu PDF bản sao") : (IsPreview ? "Mô phỏng lệnh in" : "In bản sao");
    public string OutputHint => IsPreview
        ? "Đây là bản xem thử frontend. Chưa gửi lệnh đến máy in và chưa tạo file PDF."
        : IsPdfPreview ? "Chọn Lưu PDF bản sao để lưu file PDF A4 có dấu BẢN SAO / REPRINT."
        : "Chọn In bản sao để gửi lệnh tới máy in đã chọn. Mọi bản in đều có dấu BẢN SAO / REPRINT.";
    public double ReceiptWidth => PaperWidth == 58 ? 240 : 300;

    public OrdersViewModel(InvoiceApiClient api, SessionService session, IInvoiceOutputService output) : base(session)
    { _api = api; _output = output; _ = InitializeAsync(); }
    private OrdersViewModel(SessionService session, IReadOnlyList<InvoiceViewItem> preview) : base(session)
    { _preview = preview; }
    public static OrdersViewModel CreatePreview(SessionService session) => new(session, InvoicePreviewData.Create());

    public async Task InitializeAsync()
    {
        if (_initialized || _disposed) return;
        _initialized = true;
        await ApplyFiltersAsync();
    }

    partial void OnSelectedInvoiceChanged(InvoiceViewItem? value)
    {
        if (!_settingSelection) _ = LoadDetailAsync(value);
    }
    partial void OnSelectedDetailChanged(InvoiceViewItem? value) => NotifyActions();
    partial void OnIsDetailLoadingChanged(bool value) => NotifyActions();
    partial void OnDetailErrorChanged(string? value) => OnPropertyChanged(nameof(ShowDetailPlaceholder));
    partial void OnIsOutputBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(DialogActionText)); NotifyActions();
        CloseReprintCommand.NotifyCanExecuteChanged();
    }
    partial void OnIsReprintOpenChanged(bool value) => ConfirmOutputCommand.NotifyCanExecuteChanged();
    partial void OnPaperWidthChanged(int value) => OnPropertyChanged(nameof(ReceiptWidth));
    partial void OnIsPdfPreviewChanged(bool value)
    { OnPropertyChanged(nameof(DialogTitle)); OnPropertyChanged(nameof(DialogActionText)); OnPropertyChanged(nameof(OutputHint)); }

    [RelayCommand]
    private async Task ApplyFiltersAsync()
    {
        if (FromDate.HasValue && ToDate.HasValue && FromDate.Value.Date > ToDate.Value.Date)
        { FilterError = "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc."; return; }
        if (Search.Trim().Length > 100)
        { FilterError = "Từ khóa tìm kiếm tối đa 100 ký tự."; return; }
        FilterError = null;
        var storeId = Guid.TryParse(CurrentStoreId, out var sId) && sId != Guid.Empty ? (Guid?)sId : null;
        _appliedFilter = new(StoreId: storeId, From: FromDate.HasValue ? new DateTimeOffset(FromDate.Value.Date) : null,
            To: ToDate.HasValue ? new DateTimeOffset(ToDate.Value.Date.AddDays(1).AddTicks(-1)) : null,
            PageSize: PageSize, Search: Search.Trim(), PaymentMethod: PaymentCode(PaymentFilter));
        await LoadPageAsync(1);
    }
    private static string? PaymentCode(string label) => label switch
    {
        "Tiền mặt" => "Cash", "Thẻ ngân hàng" => "Card", "Điểm thành viên" => "Points",
        "VietQR" => "VietQR", "MoMo" => "MoMo", _ => null
    };

    private async Task LoadPageAsync(int page)
    {
        if (_disposed) return;
        var version = ++_listVersion;
        _listCts?.Cancel(); _listCts?.Dispose();
        var ct = (_listCts = new()).Token;
        ClearSelection();
        Invoices.Clear(); _totalCount = 0; ListError = null; PageNumber = page;
        IsLoading = true; NotifyPaging();
        try
        {
            if (!CanRead) throw new InvalidOperationException("Bạn không có quyền xem và in hóa đơn (invoices:read).");
            IReadOnlyList<InvoiceViewItem> rows;
            if (_preview != null)
            {
                var filter = _appliedFilter;
                var filtered = _preview.Where(x =>
                    (string.IsNullOrWhiteSpace(filter.Search) || x.InvoiceNo.Contains(filter.Search, StringComparison.OrdinalIgnoreCase) || x.BuyerPhone.Contains(filter.Search))
                    && (!filter.From.HasValue || x.IssuedAt >= filter.From.Value.LocalDateTime)
                    && (!filter.To.HasValue || x.IssuedAt <= filter.To.Value.LocalDateTime)
                    && (filter.PaymentMethod == null || x.PaymentMethod == InvoiceViewItem.PaymentLabel(filter.PaymentMethod))).ToList();
                _totalCount = filtered.Count;
                rows = filtered.Skip((page - 1) * PageSize).Take(PageSize).ToArray();
            }
            else
            {
                var result = await _api!.GetAllAsync(_appliedFilter with { PageNumber = page }, ct);
                if (ct.IsCancellationRequested || version != _listVersion || _disposed) return;
                _totalCount = result.TotalCount;
                rows = result.Items.Select(SummaryItem).ToArray();
            }
            foreach (var row in rows) Invoices.Add(row);
            SelectedInvoice = Invoices.FirstOrDefault();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { if (version == _listVersion && !_disposed) ListError = ex.Message; }
        finally { if (version == _listVersion && !_disposed) { IsLoading = false; NotifyPaging(); } }
    }

    private static InvoiceViewItem SummaryItem(InvoiceSummaryResponse invoice) => new()
    {
        Id = invoice.Id, InvoiceNo = invoice.InvoiceNo, OrderNo = invoice.OrderId.ToString("N")[..8],
        IssuedAt = invoice.IssuedAt.LocalDateTime, BuyerName = invoice.BuyerName ?? "Khách lẻ",
        BuyerPhone = invoice.CustomerPhone ?? "—", CashierName = "", PaymentMethod = "", Items = [],
        SnapshotSubtotal = invoice.TotalBeforeTax, SnapshotTax = invoice.TaxAmount, SnapshotGrandTotal = invoice.GrandTotal
    };

    private async Task LoadDetailAsync(InvoiceViewItem? summary)
    {
        if (_disposed) return;
        var version = ++_detailVersion;
        _detailCts?.Cancel(); _detailCts?.Dispose();
        var ct = (_detailCts = new()).Token;
        SelectedDetail = null; DetailError = null; IsDetailLoading = summary != null;
        if (summary == null || _disposed) return;
        try
        {
            if (IsPreview) { SelectedDetail = summary; return; }
            var detailTask = _api!.GetByIdAsync(summary.Id, ct);
            var receiptTask = _api.GetReprintReceiptAsync(summary.Id, PaperWidth, ct);
            await Task.WhenAll(detailTask, receiptTask);
            if (ct.IsCancellationRequested || version != _detailVersion || _disposed) return;
            var detail = await detailTask;
            var receipt = await receiptTask;
            RequireCopy(receipt, summary.Id);
            SelectedDetail = new()
            {
                Id = detail.Id, InvoiceNo = detail.InvoiceNo, OrderNo = receipt.OrderNo,
                IssuedAt = (receipt.IssuedAt ?? detail.IssuedAt).DateTime,
                BuyerName = detail.BuyerName ?? "Khách lẻ", BuyerPhone = summary.BuyerPhone,
                CashierName = receipt.CashierName, PaymentMethod = string.Join(" + ", receipt.Payments.Select(p => InvoiceViewItem.PaymentLabel(p.Method)).Distinct()),
                StoreName = receipt.StoreName, StoreAddress = receipt.StoreAddress ?? "",
                Items = detail.Items.Select(i => new InvoiceLineItem(i.SkuCode, i.ProductName, i.Qty, i.UnitPrice, i.TotalPrice)).ToArray(),
                Discount = receipt.DiscountTotal, SnapshotSubtotal = detail.TotalBeforeTax,
                SnapshotTax = detail.TaxAmount, SnapshotGrandTotal = detail.GrandTotal
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { if (version == _detailVersion && !_disposed) DetailError = ex.Message; }
        finally { if (version == _detailVersion && !_disposed) IsDetailLoading = false; }
    }
    private static void RequireCopy(ReceiptDataResponse receipt, Guid id)
    {
        if (!receipt.IsReprint || receipt.InvoiceId != id)
            throw new InvalidOperationException("API không trả về bản sao đúng hóa đơn. Hãy tải lại dữ liệu.");
    }
    private void ClearSelection()
    {
        _detailCts?.Cancel(); ++_detailVersion; IsDetailLoading = false;
        _settingSelection = true; SelectedInvoice = null; _settingSelection = false;
        SelectedDetail = null; DetailError = null;
    }
    [RelayCommand] private async Task RetryDetailAsync() => await LoadDetailAsync(SelectedInvoice);
    [RelayCommand] private async Task RefreshAsync() => await LoadPageAsync(PageNumber);
    [RelayCommand] private async Task ClearFiltersAsync()
    { Search = ""; FromDate = null; ToDate = null; PaymentFilter = PaymentMethods[0]; await ApplyFiltersAsync(); }
    [RelayCommand] private async Task FilterTodayAsync()
    { FromDate = DateTime.Today; ToDate = DateTime.Today; await ApplyFiltersAsync(); }
    [RelayCommand] private async Task FilterWeekAsync()
    { FromDate = DateTime.Today.AddDays(-6); ToDate = DateTime.Today; await ApplyFiltersAsync(); }
    private bool CanPrevious() => PageNumber > 1 && !IsLoading;
    private bool CanNext() => PageNumber * PageSize < _totalCount && !IsLoading;
    [RelayCommand(CanExecute = nameof(CanPrevious))] private async Task PreviousPageAsync() => await LoadPageAsync(PageNumber - 1);
    [RelayCommand(CanExecute = nameof(CanNext))] private async Task NextPageAsync() => await LoadPageAsync(PageNumber + 1);
    private void NotifyPaging()
    {
        foreach (var property in new[] { nameof(IsEmpty), nameof(HasInvoices), nameof(ResultCountText), nameof(PageText), nameof(RangeText), nameof(TotalCountText), nameof(PageTotalText), nameof(VisibleCountText) }) OnPropertyChanged(property);
        PreviousPageCommand.NotifyCanExecuteChanged(); NextPageCommand.NotifyCanExecuteChanged();
    }
    private void NotifyActions()
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(ShowDetailPlaceholder));
        OpenReprintCommand.NotifyCanExecuteChanged(); PreviewPdfCommand.NotifyCanExecuteChanged(); ConfirmOutputCommand.NotifyCanExecuteChanged();
    }
    private bool CanOutput() => !_disposed && HasSelection && !IsDetailLoading && !IsOutputBusy && CanRead;
    [RelayCommand(CanExecute = nameof(CanOutput))] private void OpenReprint() => OpenOutput(false);
    [RelayCommand(CanExecute = nameof(CanOutput))] private void PreviewPdf() => OpenOutput(true);
    private void OpenOutput(bool pdf)
    {
        OutputError = null; OutputInvoice = SelectedDetail; IsPdfPreview = pdf; Copies = 1;
        PrinterOptions.Clear();
        if (!pdf && !IsPreview)
        {
            try { foreach (var printer in _output!.GetPrinterNames()) PrinterOptions.Add(printer); }
            catch (Exception ex) { OutputError = "Không đọc được danh sách máy in: " + ex.Message; }
        }
        if (PrinterOptions.Count == 0) PrinterOptions.Add(IsPreview ? "Máy in tại quầy (minh họa)" : InvoiceOutputService.ChoosePrinter);
        SelectedPrinter = PrinterOptions[0]; IsReprintOpen = true;
    }
    private bool CanClose() => !IsOutputBusy;
    [RelayCommand(CanExecute = nameof(CanClose))] private void CloseReprint()
    { IsReprintOpen = false; OutputInvoice = null; }
    private bool CanSubmit() => !_disposed && IsReprintOpen && OutputInvoice != null && !IsOutputBusy && CanRead;
    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task ConfirmOutputAsync()
    {
        if (!CanSubmit()) return;
        if (IsPreview)
        {
            if (!IsPdfPreview) OutputInvoice!.ReprintCount += Copies;
            ShowInfo(IsPdfPreview ? "Đã mô phỏng xuất PDF. Chưa tạo file." : "Đã mô phỏng in bản sao. Chưa gửi máy in.");
            CloseReprint(); return;
        }
        var invoice = OutputInvoice!;
        var context = _contextVersion;
        _outputCts?.Dispose(); var ct = (_outputCts = new()).Token;
        IsOutputBusy = true; OutputError = null;
        try
        {
            bool completed;
            if (IsPdfPreview)
            {
                var metadata = await _api!.GetReprintPdfAsync(invoice.Id, ct);
                if (metadata.InvoiceId != invoice.Id) throw new InvalidOperationException("PDF không đúng hóa đơn.");
                var bytes = await _api.DownloadReprintPdfAsync(invoice.Id, ct);
                if (ct.IsCancellationRequested || context != _contextVersion || _disposed) return;
                completed = _output!.SavePdf(bytes, metadata.InvoiceNo);
                if (completed) ShowSuccess("Đã lưu PDF bản sao có dấu BẢN SAO / REPRINT.");
            }
            else
            {
                var receipt = await _api!.GetReprintReceiptAsync(invoice.Id, PaperWidth, ct);
                if (ct.IsCancellationRequested || context != _contextVersion || _disposed) return;
                RequireCopy(receipt, invoice.Id);
                completed = _output!.PrintReceipt(receipt, SelectedPrinter, Copies);
                if (completed) ShowSuccess("Đã gửi lệnh in bản sao tới máy in.");
            }
            if (completed) { IsReprintOpen = false; OutputInvoice = null; }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { if (context == _contextVersion && !_disposed) OutputError = ex.Message; }
        finally { if (!_disposed) IsOutputBusy = false; }
    }
    protected override void OnStoreChanged(string storeId, string? storeName)
    {
        ++_contextVersion; _outputCts?.Cancel(); IsReprintOpen = false; OutputInvoice = null;
        if (!_disposed) _ = ApplyFiltersAsync();
    }
    public void Deactivate()
    {
        if (_disposed) return;
        _initialized = false; ++_contextVersion; ++_listVersion;
        _listCts?.Cancel(); _outputCts?.Cancel(); ClearSelection();
        IsReprintOpen = false; OutputInvoice = null; IsLoading = false;
        Invoices.Clear(); _totalCount = 0; NotifyPaging();
    }
    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true; ++_contextVersion; ++_listVersion; ++_detailVersion;
        foreach (var source in new[] { _listCts, _detailCts, _outputCts }) { source?.Cancel(); source?.Dispose(); }
        base.Dispose();
    }
}
