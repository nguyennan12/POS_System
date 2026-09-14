using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Application.UseCases.Auth.Errors;
using POS.Domain.Common;

namespace POS.Application.UseCases.Auth.Commands.Logout;

public class LogoutCommandHandler : ICommandHandler<LogoutCommand>
{
  private readonly ITokenService _tokenService;
  private readonly IUnitOfWork _unitOfWork;
  private readonly IRefreshTokenRepository _refreshTokenRepository;
  public LogoutCommandHandler(
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
    _tokenService = tokenService;
    _refreshTokenRepository = refreshTokenRepository;
  }
  public async Task<Result> Handle(
    LogoutCommand command,
    CancellationToken cancellationToken)
  {
    var tokenHash = _tokenService.HashRefreshToken(command.RefreshToken);

    var storedToken = await _refreshTokenRepository.GetByTokenHashWithEmployeeAsync(tokenHash, cancellationToken);

    if (storedToken is null)
      return Result.Failure(AuthErrors.InvalidRefreshToken);

    if (storedToken.RevokedAt is null)
    {
      storedToken.Revoke(DateTime.UtcNow);
      await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    return Result.Success();
  }
}
