using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Auth.Dtos;
using POS.Application.UseCases.Auth.Errors;
using POS.Domain.Common;
using POS.Domain.Employees;

namespace POS.Application.UseCases.Auth.Commands.EmployeeLoginWithPin;

public class EmployeeLoginWithPinCommandHandler : ICommandHandler<EmployeeLoginWithPinCommand, AuthDto>
{
  private readonly IEmployeeRepository _employeeRepository;
  private readonly IUnitOfWork _unitOfWork;
  private readonly IPasswordHasher _passwordHasher;
  private readonly IPermissionRepository _permissionRepository;
  private readonly ITokenService _tokenService;
  private readonly IRefreshTokenRepository _refreshTokenRepository;
  private readonly IPinLookupHasher _pinLookupHasher;
  private readonly IPinLoginRateLimiter _pinLoginRateLimiter;

  public EmployeeLoginWithPinCommandHandler(
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork, IPasswordHasher passwordHasher,
    IPermissionRepository permissionRepository,
    ITokenService tokenService,
    IPinLookupHasher pinLookupHasher,
    IRefreshTokenRepository refreshTokenRepository,
    IPinLoginRateLimiter pinLoginRateLimiter)
  {
    _employeeRepository = employeeRepository;
    _unitOfWork = unitOfWork;
    _passwordHasher = passwordHasher;
    _permissionRepository = permissionRepository;
    _tokenService = tokenService;
    _refreshTokenRepository = refreshTokenRepository;
    _pinLookupHasher = pinLookupHasher;
    _pinLoginRateLimiter = pinLoginRateLimiter;
  }

  public async Task<Result<AuthDto>> Handle(EmployeeLoginWithPinCommand command, CancellationToken cancellationToken)
  {
    if (await _pinLoginRateLimiter.IsBlockedAsync(command.StoreId, command.DeviceId, cancellationToken))
      return AuthErrors.PinLoginRateLimited;

    var now = DateTime.UtcNow;
    var pinLookupHash = _pinLookupHasher.ComputeHash(command.Pin);
    var employee = await _employeeRepository.GetByStoreAndPinLookupHashWithRoleAndStoreAsync(command.StoreId, pinLookupHash, cancellationToken);

    if (employee is null)
    {
      await _pinLoginRateLimiter.RegisterFailedAttemptAsync(command.StoreId, command.DeviceId, cancellationToken);
      return AuthErrors.InvalidCredentials;
    }

    if (!employee.IsActive) return AuthErrors.InvalidCredentials;
    if (employee.IsLocked(now)) return AuthErrors.AccountLocked;

    if (!_passwordHasher.Verify(command.Pin, employee.PinHash))
    {
      employee.RegisterFailedLogin(now);
      await _pinLoginRateLimiter.RegisterFailedAttemptAsync(command.StoreId, command.DeviceId, cancellationToken);
      await _unitOfWork.SaveChangesAsync(cancellationToken);
      return AuthErrors.InvalidCredentials;
    }

    employee.RegisterSuccessfulLogin(now);
    await _pinLoginRateLimiter.ResetAsync(command.StoreId, command.DeviceId, cancellationToken);

    var permissions = await _permissionRepository.GetPermissionCodesAsync(employee.RoleId, cancellationToken);
    var subject = new TokenSubject(
      employee.Id,
      "Employee",
      employee.RoleId,
      employee.StoreId,
      employee.IsChainOwner,
      permissions
    );

    var accessToken = _tokenService.CreateAccessToken(subject);
    var refreshToken = _tokenService.CreateRefreshToken();

    await _refreshTokenRepository.AddAsync(
      new RefreshToken(
      employee.Id,
      employee,
      refreshToken.RefreshTokenHash,
      refreshToken.ExpiresAt), cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return AuthDto.ToDto(
      employee,
      accessToken.AccessToken,
      refreshToken.RefreshToken,
      accessToken.ExpiresAt,
      permissions);
  }
}
