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
        "Tài khoản đang bị khóa.");
}