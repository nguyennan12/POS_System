using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Auth.Dtos;
using POS.Application.UseCases.Auth.Errors;
using POS.Domain.Common;
using POS.Domain.Employees;

namespace POS.Application.UseCases.Auth.Commands.EmployeeLoginWithPassword;

public class EmployeeLoginWithPasswordCommandHandler : ICommandHandler<EmployeeLoginWithPasswordCommand, AuthDto>
{
  private readonly IEmployeeRepository _employeeRepository;
  private readonly IUnitOfWork _unitOfWork;
  private readonly IPasswordHasher _passwordHasher;
  private readonly IPermissionRepository _permissionRepository;
  private readonly ITokenService _tokenService;
  private readonly IRefreshTokenRepository _refreshTokenRepository;

  public EmployeeLoginWithPasswordCommandHandler(
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork, IPasswordHasher passwordHasher,
    IPermissionRepository permissionRepository,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository)
  {
    _employeeRepository = employeeRepository;
    _unitOfWork = unitOfWork;
    _passwordHasher = passwordHasher;
    _permissionRepository = permissionRepository;
    _tokenService = tokenService;
    _refreshTokenRepository = refreshTokenRepository;
  }
  public async Task<Result<AuthDto>> Handle(EmployeeLoginWithPasswordCommand command, CancellationToken cancellationToken)
  {
    var now = DateTime.UtcNow;
    var employee = await _employeeRepository.GetByUsernameWithRoleAndStoreAsync(command.Username, cancellationToken);

    if (employee is null) return AuthErrors.InvalidCredentials;
    if (!employee.IsActive) return AuthErrors.InvalidCredentials;
    if (employee.IsLocked(now)) return AuthErrors.AccountLocked;

    if (!_passwordHasher.Verify(command.Password, employee.PasswordHash))
    {
      employee.RegisterFailedLogin(now);
      await _unitOfWork.SaveChangesAsync(cancellationToken);
      return AuthErrors.InvalidCredentials;
    }

    employee.RegisterSuccessfulLogin(now);

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
