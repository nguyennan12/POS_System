using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Contracts.V1.Config;
using POS.Domain.Common;
using POS.Domain.Configuration;

namespace POS.Application.UseCases.Config.Commands.UpdateStoreConfig;

public class UpdateStoreConfigCommandHandler(
    ISystemConfigRepository configs,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateStoreConfigCommand, StoreConfigResponse>
{
    public async Task<Result<StoreConfigResponse>> Handle(
        UpdateStoreConfigCommand command, CancellationToken cancellationToken)
    {
        var existing = await configs.GetByStoreAsync(command.StoreId, cancellationToken);
        var existingMap = existing.ToDictionary(c => c.Key);

        foreach (var (key, value) in command.Configs)
        {
            if (existingMap.TryGetValue(key, out var config))
                config.SetValue(value);
            else
                await configs.AddAsync(SystemConfig.Create(command.StoreId, key, value), cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Re-fetch to return current state
        var updated = await configs.GetByStoreAsync(command.StoreId, cancellationToken);
        var dict = updated.ToDictionary(c => c.Key, c => c.Value);
        return new StoreConfigResponse(command.StoreId, dict);
    }
}
