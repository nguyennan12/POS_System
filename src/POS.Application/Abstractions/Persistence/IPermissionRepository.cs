namespace POS.Application.Abstractions.Persistence;

public interface IPermissionRepository
{
  Task<IReadOnlyList<string>> GetPermissionCodesAsync(
      Guid roleId,
      CancellationToken cancellationToken = default);
}