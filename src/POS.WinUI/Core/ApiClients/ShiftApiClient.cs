using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using POS.Contracts.V1.Common;
using POS.Contracts.V1.Shifts;
using POS.WinUI.Core.Constants;
using POS.WinUI.Core.Services;

namespace POS.WinUI.Core.ApiClients;

///  
/// Client gọi REST API cho tài nguyên ca làm việc 'shifts'
/// </summary>
public sealed class ShiftApiClient : BaseApiClient
{
  public ShiftApiClient(HttpClient http, SessionService session)
      : base(http, session) { }


  /// Lấy thông tin ca làm việc đang mở hiện tại của một cửa hàng (kèm thống kê doanh thu tức thời)
  /// </summary>
  public Task<ApiResponse<ShiftSummaryResponse>?> GetCurrentShiftAsync(Guid storeId, CancellationToken ct = default)
      => GetAsync<ApiResponse<ShiftSummaryResponse>>($"{ApiRoutes.Shifts.Current}?storeId={storeId}", ct);


  /// Mở ca làm việc mới kèm số tiền mặt ban đầu
  /// </summary>
  public Task<ApiResponse<ShiftResponse>?> OpenShiftAsync(OpenShiftRequest request, CancellationToken ct = default)
      => PostAsync<ApiResponse<ShiftResponse>>(ApiRoutes.Shifts.Open, request, ct);


  /// Đóng/chốt ca làm việc và kiểm kê tiền mặt thực tế
  /// </summary>
  public Task<ApiResponse<ShiftSummaryResponse>?> CloseShiftAsync(Guid shiftId, CloseShiftRequest request, CancellationToken ct = default)
      => PostAsync<ApiResponse<ShiftSummaryResponse>>(ApiRoutes.Shifts.Close(shiftId), request, ct);


  /// Lấy chi tiết ca làm việc theo ID
  /// </summary>
  public Task<ApiResponse<ShiftResponse>?> GetShiftByIdAsync(Guid shiftId, CancellationToken ct = default)
      => GetAsync<ApiResponse<ShiftResponse>>(ApiRoutes.Shifts.GetById(shiftId), ct);
}
