namespace POS.Contracts.V1.Auth;

public record LoginRequest(
    string Username,
    string Password
);

public record PinLoginRequest(
    string Pin,
    Guid? StoreId = null
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record LogoutRequest(
    string RefreshToken
);

public record ChangePinRequest(
    string OldPin,
    string NewPin
);

public record ChangePasswordRequest(
    string OldPassword,
    string NewPassword
);
