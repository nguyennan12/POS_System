using POS.Domain.Common;

namespace POS.Application.UseCases.Auth.Errors;

public static class AuthErrors
{
  public static readonly Error InvalidCredentials = new(
        ErrorType.Unauthorized,
        "AUTH.INVALID_CREDENTIALS",
        "Thông tin đăng nhập không hợp lệ.");

  public static readonly Error AccountLocked = new(
        ErrorType.Forbidden,
        "AUTH.ACCOUNT_LOCKED",
        "Thiết bị đăng nhập PIN sai quá nhiều lần. Vui lòng thử lại sau 15 phút.");

  public static readonly Error PinLoginRateLimited = new(
        ErrorType.Forbidden,
        "AUTH.PIN_LOGIN_RATE_LIMITED",
        "Thiết bị đăng nhập PIN sai quá nhiều lần. Vui lòng thử lại sau 15 phút.");

  public static readonly Error InvalidRefreshToken = new(
      ErrorType.Unauthorized,
      "AUTH.INVALID_REFRESH_TOKEN",
      "Refresh token không hợp lệ.");

  public static readonly Error RefreshTokenExpired = new(
      ErrorType.Unauthorized,
      "AUTH.REFRESH_TOKEN_EXPIRED",
      "Refresh token đã hết hạn.");

  public static readonly Error RefreshTokenRevoked = new(
      ErrorType.Unauthorized,
      "AUTH.REFRESH_TOKEN_REVOKED",
      "Refresh token đã bị thu hồi.");

  public static readonly Error InvalidCurrentPassword = new(
      ErrorType.Unauthorized,
      "AUTH.INVALID_CURRENT_PASSWORD",
      "Mật khẩu hiện tại không chính xác.");

  public static readonly Error InvalidCurrentPin = new(
      ErrorType.Unauthorized,
      "AUTH.INVALID_CURRENT_PIN",
      "Mã PIN hiện tại không chính xác.");

  public static readonly Error NewPasswordMustBeDifferent = new(
      ErrorType.Validation,
      "AUTH.PASSWORD_SAME_AS_OLD",
      "Mật khẩu mới không được trùng với mật khẩu hiện tại.");

  public static readonly Error NewPinMustBeDifferent = new(
      ErrorType.Validation,
      "AUTH.PIN_SAME_AS_OLD",
      "Mã PIN mới không được trùng với mã PIN hiện tại.");

  public static readonly Error Unauthorized = new(
      ErrorType.Unauthorized,
      "AUTH.UNAUTHORIZED",
      "Yêu cầu xác thực danh tính.");
}