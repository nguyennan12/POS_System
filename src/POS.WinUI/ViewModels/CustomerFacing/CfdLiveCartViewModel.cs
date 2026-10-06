using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using POS.WinUI.Core.Models.Cfd;
using POS.WinUI.Core.Services;

namespace POS.WinUI.ViewModels.CustomerFacing;

public sealed partial class CfdLiveCartViewModel : ObservableObject, IRecipient<CfdCartStateMessage>, IRecipient<CfdShowStandbyMessage>
{
  private readonly SessionService _sessionService;

  [ObservableProperty] private string _storeBranchName = "Retail Operations";
  [ObservableProperty] private string _welcomeStatus = "Đang phục vụ";
  [ObservableProperty] private string _wifiInfo = "OraPOS_Guest";
  [ObservableProperty] private string _statusText = "Đang xử lý đơn hàng";
  [ObservableProperty] private int _totalItems;
  [ObservableProperty] private decimal _subTotal;
  [ObservableProperty] private decimal _discountAmount;
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

  public CfdLiveCartViewModel(SessionService sessionService)
  {
    _sessionService = sessionService;

    if (!string.IsNullOrWhiteSpace(_sessionService.StoreName))
    {
      StoreBranchName = _sessionService.StoreName.ToUpperInvariant();
    }

    // Đăng ký nhận tin giỏ hàng từ In-Memory Messenger ngay khi khởi tạo
    WeakReferenceMessenger.Default.Register<CfdCartStateMessage>(this);
    WeakReferenceMessenger.Default.Register<CfdShowStandbyMessage>(this);
  }

  public void Receive(CfdCartStateMessage message)
  {
    void Update()
    {
      var state = message.State;

      TotalItems = state.TotalItems;
      SubTotal = state.SubTotal;
      DiscountAmount = state.DiscountAmount;
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
    void Update()
    {
      CartItems.Clear();
      TotalItems = 0;
      SubTotal = 0;
      DiscountAmount = 0;
      GrandTotal = 0;
      LatestItem = null;
      CustomerName = null;
      CustomerPhone = null;
      CustomerTier = "Thành viên";
      CustomerPointsBalance = 0;
      PointsEarned = 0;
      HasCustomerInfo = false;
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
}
