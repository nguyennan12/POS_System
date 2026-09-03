namespace POS.WinUI.Services;

/// <summary>
/// Quản lý JWT token và thông tin user hiện tại trong suốt phiên làm việc.
/// Token được lưu trong memory (không persistent) — mỗi lần khởi động lại app cần đăng nhập lại.
/// </summary>
public sealed class SessionService
{
  public string? AccessToken { get; private set; }
  public string? RefreshToken { get; private set; }
  public string? EmployeeId { get; private set; }
  public string? EmployeeName { get; private set; }
  public string? Role { get; private set; }
  public string? StoreId { get; private set; }
  public string? ShiftId { get; private set; }
  public bool IsLoggedIn => !string.IsNullOrEmpty(AccessToken);

  public void SetSession(string accessToken, string refreshToken,
      string employeeId, string employeeName, string role, string storeId)
  {
    AccessToken = accessToken;
    RefreshToken = refreshToken;
    EmployeeId = employeeId;
    EmployeeName = employeeName;
    Role = role;
    StoreId = storeId;
  }

  public void SetShift(string shiftId) => ShiftId = shiftId;

  public void RefreshAccessToken(string newAccessToken) => AccessToken = newAccessToken;

  public void Clear()
  {
    AccessToken = null;
    RefreshToken = null;
    EmployeeId = null;
    EmployeeName = null;
    Role = null;
    StoreId = null;
    ShiftId = null;
  }
}
