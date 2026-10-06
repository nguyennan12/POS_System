using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using POS.WinUI.Core.Models.Cfd;
using POS.WinUI.Core.Services;

namespace POS.WinUI.ViewModels.CustomerFacing;

public sealed partial class CfdLiveCartViewModel : ObservableObject, 
    IRecipient<CfdCartStateMessage>, 
    IRecipient<CfdShowStandbyMessage>,
    IRecipient<CfdPaymentStateMessage>,
    IRecipient<CfdClosePaymentMessage>
{
  private readonly SessionService _sessionService;
  private DispatcherTimer? _successDismissTimer;

  // ════════════════════ HEADER & STORE INFO ════════════════════
  [ObservableProperty] private string _storeBranchName = "Retail Operations";
  [ObservableProperty] private string _welcomeStatus = "Đang phục vụ";
  [ObservableProperty] private string _wifiInfo = "OraPOS_Guest";
  [ObservableProperty] private string _statusText = "Đang xử lý đơn hàng";

  // ════════════════════ CART STATE ════════════════════
  [ObservableProperty] private int _totalItems;
  [ObservableProperty] private decimal _subTotal;
  [ObservableProperty] private decimal _discountAmount;
  [ObservableProperty] private decimal _voucherDiscount;
  [ObservableProperty] private decimal _pointsDiscount;
  [ObservableProperty] private decimal _pointsUsed;
  [ObservableProperty] private decimal _pointsBalanceRemaining;
  [ObservableProperty] private decimal _taxAmount;
  [ObservableProperty] private decimal _grandTotal;

  [ObservableProperty] private string? _customerName;
  [ObservableProperty] private string? _customerPhone;
  [ObservableProperty] private string? _customerTier = "Thành viên";
  [ObservableProperty] private decimal _customerPointsBalance;
  [ObservableProperty] private decimal _pointsEarned;
  [ObservableProperty] private bool _hasCustomerInfo;

  [ObservableProperty] private CfdCartItemDto? _latestItem;

  public ObservableCollection<CfdCartItemDto> CartItems { get; } = new();

  // ════════════════════ PAYMENT MODE STATE ════════════════════
  [ObservableProperty] private bool _isPaymentMode;
  [ObservableProperty] private string _paymentMethod = "Cash";
  [ObservableProperty] private decimal _paymentGrandTotal;
  [ObservableProperty] private decimal _paymentTenderedCash;
  [ObservableProperty] private decimal _paymentChangeAmount;
  [ObservableProperty] private string? _paymentQrDataUrl;
  [ObservableProperty] private string? _paymentOrderCode;
  [ObservableProperty] private string? _paymentBankName = "MB Bank";
  [ObservableProperty] private string? _paymentAccountNumber = "0988888888";
  [ObservableProperty] private string? _paymentAccountName = "CỬA HÀNG POS";
  [ObservableProperty] private string? _paymentTransferContent = "THANHTOAN";
  [ObservableProperty] private string _paymentStatusText = "Đang chờ thanh toán...";
  [ObservableProperty] private bool _isPaymentCompleted;
  [ObservableProperty] private string _paymentSuccessTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
  [ObservableProperty] private BitmapSource? _paymentQrImage;

  // Helper flags for payment methods & discounts
  public bool IsVietQr => string.Equals(PaymentMethod, "VietQR", StringComparison.OrdinalIgnoreCase);
  public bool IsMoMo => string.Equals(PaymentMethod, "MoMo", StringComparison.OrdinalIgnoreCase);
  public bool IsCash => string.Equals(PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase);
  public bool IsCard => string.Equals(PaymentMethod, "Card", StringComparison.OrdinalIgnoreCase);
  public bool IsPoints => string.Equals(PaymentMethod, "Points", StringComparison.OrdinalIgnoreCase);

  public bool HasPointsDiscount => PointsDiscount > 0 || PointsUsed > 0;
  public bool HasVoucherDiscount => VoucherDiscount > 0;

  public string PaymentMethodBadgeTitle => PaymentMethod?.ToUpperInvariant() switch
  {
    "VIETQR" => "CHUYỂN KHOẢN VIETQR 24/7",
    "MOMO" => "VÍ ĐIỆN TỬ MOMO",
    "CARD" => "THẺ NGÂN HÀNG / POS",
    "POINTS" => "ĐIỂM THƯỞNG ORACLUB",
    _ => "TIỀN MẶT TẠI QUẦY"
  };

  public CfdLiveCartViewModel(SessionService sessionService)
  {
    _sessionService = sessionService;

    if (!string.IsNullOrWhiteSpace(_sessionService.StoreName))
    {
      StoreBranchName = _sessionService.StoreName.ToUpperInvariant();
    }

    // Đăng ký nhận tin giỏ hàng & thanh toán từ In-Memory Messenger ngay khi khởi tạo
    WeakReferenceMessenger.Default.Register<CfdCartStateMessage>(this);
    WeakReferenceMessenger.Default.Register<CfdShowStandbyMessage>(this);
    WeakReferenceMessenger.Default.Register<CfdPaymentStateMessage>(this);
    WeakReferenceMessenger.Default.Register<CfdClosePaymentMessage>(this);
  }

  public void Receive(CfdCartStateMessage message)
  {
    void Update()
    {
      var state = message.State;

      // Hủy timer tự đóng nếu khách/thu ngân quét món mới
      _successDismissTimer?.Stop();
      _successDismissTimer = null;
      IsPaymentCompleted = false;

      TotalItems = state.TotalItems;
      SubTotal = state.SubTotal;
      DiscountAmount = state.DiscountAmount;
      VoucherDiscount = state.VoucherDiscount;
      PointsDiscount = state.PointsDiscount;
      PointsUsed = state.PointsUsed;
      TaxAmount = state.TaxAmount;
      GrandTotal = state.GrandTotal;

      CustomerName = state.CustomerName;
      CustomerPhone = state.CustomerPhone;
      CustomerTier = !string.IsNullOrWhiteSpace(state.CustomerTier) ? state.CustomerTier : "Thành viên";
      CustomerPointsBalance = state.CustomerPointsBalance;
      PointsEarned = state.PointsEarned;
      HasCustomerInfo = !string.IsNullOrWhiteSpace(state.CustomerName);

      // Cập nhật danh sách món (món mới nhất lên đầu)
      CartItems.Clear();
      if (state.Items != null && state.Items.Count > 0)
      {
        for (int i = state.Items.Count - 1; i >= 0; i--)
        {
          CartItems.Add(state.Items[i]);
        }
        LatestItem = state.Items.Last();
      }
      else
      {
        LatestItem = null;
      }

      NotifyPaymentMethodChanged();
    }

    if (Application.Current?.Dispatcher?.CheckAccess() == false)
    {
      Application.Current.Dispatcher.Invoke(Update);
    }
    else
    {
      Update();
    }
  }

  public void Receive(CfdPaymentStateMessage message)
  {
    void Update()
    {
      var state = message.State;
      IsPaymentMode = true;
      PaymentMethod = state.PaymentMethod ?? "Cash";
      GrandTotal = state.GrandTotal;
      PaymentGrandTotal = state.GrandTotal;
      PaymentTenderedCash = state.TenderedCash;
      PaymentChangeAmount = state.ChangeAmount;
      PaymentQrDataUrl = state.QrDataUrl;
      PaymentOrderCode = state.OrderCode;
      PaymentBankName = !string.IsNullOrWhiteSpace(state.BankName) ? state.BankName : "MB Bank";
      PaymentAccountNumber = !string.IsNullOrWhiteSpace(state.AccountNumber) ? state.AccountNumber : "0988888888";
      PaymentAccountName = !string.IsNullOrWhiteSpace(state.AccountName) ? state.AccountName : "CỬA HÀNG POS";
      PaymentTransferContent = !string.IsNullOrWhiteSpace(state.TransferContent) ? state.TransferContent : "THANHTOAN";
      PaymentStatusText = !string.IsNullOrWhiteSpace(state.StatusText) ? state.StatusText : (state.IsCompleted ? "Thanh toán thành công! ✅" : "Đang chờ thanh toán...");
      IsPaymentCompleted = state.IsCompleted;

      // Cập nhật thông tin giảm giá điểm và voucher
      PointsUsed = state.PointsUsed;
      PointsDiscount = state.PointsDiscount;
      VoucherDiscount = state.VoucherDiscount;
      DiscountAmount = state.VoucherDiscount + state.PointsDiscount;
      PointsBalanceRemaining = state.PointsBalanceRemaining;
      if (state.PointsEarned > 0)
      {
        PointsEarned = state.PointsEarned;
      }
      if (!string.IsNullOrWhiteSpace(state.CustomerName))
      {
        CustomerName = state.CustomerName;
        CustomerPhone = state.CustomerPhone;
        CustomerTier = state.CustomerTier ?? "Thành viên";
        HasCustomerInfo = true;
      }
      if (state.SubTotal > 0)
      {
        SubTotal = state.SubTotal;
      }

      if (state.IsCompleted)
      {
        PaymentSuccessTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        StartSuccessDismissTimer();
      }

      // Sinh mã QR nội bộ nhanh chóng nếu có payload
      if (!string.IsNullOrWhiteSpace(state.QrDataUrl))
      {
        PaymentQrImage = GenerateQrBitmap(state.QrDataUrl, 260);
      }
      else if (IsVietQr)
      {
        var qrPayload = $"https://img.vietqr.io/image/{PaymentBankName}-{PaymentAccountNumber}-compact2.png?amount={(long)PaymentGrandTotal}&addInfo={PaymentTransferContent}&accountName={Uri.EscapeDataString(PaymentAccountName ?? string.Empty)}";
        PaymentQrImage = GenerateQrBitmap(qrPayload, 260);
      }
      else if (IsMoMo)
      {
        var momoPayload = $"2|99|{PaymentAccountNumber}|{PaymentAccountName}|pos@store.vn|0|0|{(long)PaymentGrandTotal}|{PaymentTransferContent}|transfer_myqr";
        PaymentQrImage = GenerateQrBitmap(momoPayload, 260);
      }
      else
      {
        PaymentQrImage = null;
      }

      NotifyPaymentMethodChanged();
    }

    if (Application.Current?.Dispatcher?.CheckAccess() == false)
    {
      Application.Current.Dispatcher.Invoke(Update);
    }
    else
    {
      Update();
    }
  }

  public void Receive(CfdClosePaymentMessage message)
  {
    void Update()
    {
      _successDismissTimer?.Stop();
      _successDismissTimer = null;
      IsPaymentMode = false;
      IsPaymentCompleted = false;
      PaymentQrImage = null;
      NotifyPaymentMethodChanged();
    }

    if (Application.Current?.Dispatcher?.CheckAccess() == false)
    {
      Application.Current.Dispatcher.Invoke(Update);
    }
    else
    {
      Update();
    }
  }

  public void Receive(CfdShowStandbyMessage message)
  {
    // Nếu đang trong màn hình thanh toán thành công, không chuyển về standby ngay để khách kịp xem thông tin
    if (IsPaymentCompleted) return;

    void Update()
    {
      ResetToStandby();
    }

    if (Application.Current?.Dispatcher?.CheckAccess() == false)
    {
      Application.Current.Dispatcher.Invoke(Update);
    }
    else
    {
      Update();
    }
  }

  private void StartSuccessDismissTimer()
  {
    _successDismissTimer?.Stop();
    _successDismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
    _successDismissTimer.Tick += (_, _) =>
    {
      _successDismissTimer?.Stop();
      _successDismissTimer = null;
      if (IsPaymentCompleted)
      {
        ResetToStandby();
      }
    };
    _successDismissTimer.Start();
  }

  private void ResetToStandby()
  {
    IsPaymentMode = false;
    IsPaymentCompleted = false;
    PaymentQrImage = null;
    CartItems.Clear();
    TotalItems = 0;
    SubTotal = 0;
    DiscountAmount = 0;
    VoucherDiscount = 0;
    PointsDiscount = 0;
    PointsUsed = 0;
    PointsBalanceRemaining = 0;
    GrandTotal = 0;
    LatestItem = null;
    CustomerName = null;
    CustomerPhone = null;
    CustomerTier = "Thành viên";
    CustomerPointsBalance = 0;
    PointsEarned = 0;
    HasCustomerInfo = false;
    NotifyPaymentMethodChanged();
  }

  private void NotifyPaymentMethodChanged()
  {
    OnPropertyChanged(nameof(IsVietQr));
    OnPropertyChanged(nameof(IsMoMo));
    OnPropertyChanged(nameof(IsCash));
    OnPropertyChanged(nameof(IsCard));
    OnPropertyChanged(nameof(IsPoints));
    OnPropertyChanged(nameof(HasPointsDiscount));
    OnPropertyChanged(nameof(HasVoucherDiscount));
    OnPropertyChanged(nameof(PaymentMethodBadgeTitle));
  }

  private static BitmapSource? GenerateQrBitmap(string content, int size = 260)
  {
    try
    {
      var writer = new ZXing.BarcodeWriterPixelData
      {
        Format = ZXing.BarcodeFormat.QR_CODE,
        Options = new ZXing.QrCode.QrCodeEncodingOptions
        {
          Height = size,
          Width = size,
          Margin = 1
        }
      };
      var pixelData = writer.Write(content);
      var bmp = BitmapSource.Create(
          pixelData.Width,
          pixelData.Height,
          96,
          96,
          PixelFormats.Bgra32,
          null,
          pixelData.Pixels,
          pixelData.Width * 4);

      bmp.Freeze();
      return bmp;
    }
    catch
    {
      return null;
    }
  }
}
