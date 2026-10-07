using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

///  
/// Http DelegatingHandler tự động phát hiện tình trạng mạng thông qua các API request thực tế (Passive Detection).
/// </summary>
public sealed class NetworkStatusHandler : DelegatingHandler
{
  private readonly NetworkStatusService _networkStatusService;

  public NetworkStatusHandler(NetworkStatusService networkStatusService)
  {
    _networkStatusService = networkStatusService;
  }

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    try
    {
      var response = await base.SendAsync(request, cancellationToken);

      // Server phản hồi thành công hoặc lỗi nghiệp vụ (400, 401, 403...) => Server đang sống
      if (response.IsSuccessStatusCode ||
          (response.StatusCode < HttpStatusCode.InternalServerError && response.StatusCode != HttpStatusCode.RequestTimeout))
      {
        _networkStatusService.ReportSuccess();
      }
      else if (response.StatusCode == HttpStatusCode.ServiceUnavailable ||
               response.StatusCode == HttpStatusCode.GatewayTimeout ||
               response.StatusCode == HttpStatusCode.BadGateway)
      {
        _networkStatusService.ReportFailure();
      }

      return response;
    }
    catch (HttpRequestException)
    {
      _networkStatusService.ReportFailure();
      throw;
    }
    catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
    {
      // Timeout do mạng hoặc server không phản hồi
      _networkStatusService.ReportFailure();
      throw new TimeoutException("Yêu cầu đến máy chủ đã hết thời gian chờ.", ex);
    }
  }
}
