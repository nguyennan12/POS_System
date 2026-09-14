using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Auth.Dtos;
using POS.Application.UseCases.Auth.Errors;
using POS.Domain.Common;
using POS.Domain.Employees;

namespace POS.Application.UseCases.Auth.Commands.Refresh;

public class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, AuthDto>
{
  private readonly ITokenService _tokenService;
  private readonly IUnitOfWork _unitOfWork;
  private readonly IRefreshTokenRepository _refreshTokenRepository;
  private readonly IPermissionRepository _permissionRepository;
  public RefreshTokenCommandHandler(
    ITokenService tokenService,
     IRefreshTokenRepository refreshTokenRepository,
     IPermissionRepository permissionRepository,
     IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
    _tokenService = tokenService;
    _refreshTokenRepository = refreshTokenRepository;
    _permissionRepository = permissionRepository;
  }
  public async Task<Result<AuthDto>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
  {
    var now = DateTime.UtcNow;
    var tokenHash = _tokenService.HashRefreshToken(command.RefreshToken);
    var storedToken = await _refreshTokenRepository.GetByTokenHashWithEmployeeAsync(tokenHash, cancellationToken);

    if (storedToken is null) return AuthErrors.InvalidRefreshToken;
    if (storedToken.RevokedAt is not null) return AuthErrors.RefreshTokenRevoked;
    if (storedToken.ExpiresAt <= now) return AuthErrors.RefreshTokenExpired;

    var employee = storedToken.Employee;
    if (!employee.IsActive) return AuthErrors.InvalidCredentials;

    storedToken.Revoke(now);
    var permissions = await _permissionRepository.GetPermissionCodesAsync(employee.RoleId, cancellationToken);

    var subject = new TokenSubject(
      employee.Id,
      "Employee",
      employee.RoleId,
      employee.StoreId,
      employee.IsChainOwner,
      permissions);

    var accessToken = _tokenService.CreateAccessToken(subject);
    var newRefreshToken = _tokenService.CreateRefreshToken();

    await _refreshTokenRepository.AddAsync(
        new RefreshToken(
            employee.Id,
            employee,
            newRefreshToken.RefreshTokenHash,
            newRefreshToken.ExpiresAt),
        cancellationToken);

    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return AuthDto.ToDto(
        employee,
        accessToken.AccessToken,
        newRefreshToken.RefreshToken,
        accessToken.ExpiresAt,
        permissions);
  }
}
