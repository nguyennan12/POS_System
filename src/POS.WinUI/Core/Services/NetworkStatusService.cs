using System;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace POS.WinUI.Core.Services;

public sealed class NetworkStatusService : IDisposable
{
  private readonly HttpClient _fastHealthClient;
  private readonly SemaphoreSlim _probeLock = new(1, 1);
  private CancellationTokenSource? _probeCts;
  private bool _isDisposed;

  public bool IsOnline { get; private set; } = true;
  public string StatusText => IsOnline ? "Đã kết nối máy chủ" : "Mất kết nối máy chủ";

  public event Action<bool>? StatusChanged;

  public NetworkStatusService(IConfiguration configuration)
  {
    var apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000/";

    // health check với timeout 3 giây
    _fastHealthClient = new HttpClient
    {
      BaseAddress = new Uri(apiBaseUrl),
      Timeout = TimeSpan.FromSeconds(3)
    };

    // Lắng nghe sự kiện ngắt Wifi từ Windows
    NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;

    // Khởi động kiểm tra ngay lần đầu
    _ = CheckHealthAsync();
  }

  private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
  {
    if (!e.IsAvailable)
    {
      ReportFailure();
    }
    else
    {
      _ = CheckHealthAsync();
    }
  }


  /// Được gọi bởi Http Handler khi bất kỳ API nào thành công.
  /// </summary>
  public void ReportSuccess()
  {
    if (!IsOnline)
    {
      IsOnline = true;
      StopProbing();
      StatusChanged?.Invoke(true);
    }
  }


  /// Được gọi bởi Http Handler khi API gặp lỗi kết nối hoặc server sập.
  /// </summary>
  public void ReportFailure()
  {
    if (IsOnline)
    {
      IsOnline = false;
      StatusChanged?.Invoke(false);
    }
    StartProbing();
  }


  /// Kiểm tra thủ công hoặc tức thời trạng thái máy chủ.
  /// </summary>
  public async Task<bool> CheckHealthAsync()
  {
    try
    {
      using var response = await _fastHealthClient.GetAsync("health");
      if (response.IsSuccessStatusCode)
      {
        ReportSuccess();
        return true;
      }
    }
    catch
    {
      // Kết nối thất bại
    }

    ReportFailure();
    return false;
  }

  private void StartProbing()
  {
    if (_isDisposed) return;

    lock (_probeLock)
    {
      if (_probeCts != null) return; // Đang chạy probing rồi
      _probeCts = new CancellationTokenSource();
    }

    _ = ProbeLoopAsync(_probeCts.Token);
  }

  private void StopProbing()
  {
    lock (_probeLock)
    {
      _probeCts?.Cancel();
      _probeCts = null;
    }
  }

  private async Task ProbeLoopAsync(CancellationToken ct)
  {
    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
    while (!ct.IsCancellationRequested)
    {
      try
      {
        await timer.WaitForNextTickAsync(ct);
        if (ct.IsCancellationRequested) break;

        using var response = await _fastHealthClient.GetAsync("health", ct);
        if (response.IsSuccessStatusCode)
        {
          ReportSuccess();
          break; // Thoát vòng lặp khi đã online trở lại
        }
      }
      catch (OperationCanceledException)
      {
        break;
      }
      catch
      {
        // Vẫn đang offline, tiếp tục đợi chu kỳ tiếp theo
      }
    }
  }

  public void Dispose()
  {
    if (_isDisposed) return;
    _isDisposed = true;

    NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
    StopProbing();
    _fastHealthClient.Dispose();
    _probeLock.Dispose();
  }
}
