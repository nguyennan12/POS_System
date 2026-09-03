using POS.Application.Abstractions.Persistence;

namespace POS.Api.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        using var scope = app.Services.CreateScope();
        var migrationService = scope.ServiceProvider.GetRequiredService<IMigrationService>();
        await migrationService.ExecuteAsync(cancellationToken);
    }
}
