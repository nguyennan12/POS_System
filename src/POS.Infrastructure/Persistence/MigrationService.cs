using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Persistence;
using POS.Infrastructure.Persistence.Seeders;

namespace POS.Infrastructure.Persistence;

public class MigrationService : IMigrationService
{
  private readonly AppDbContext _context;
  private readonly ILogger<MigrationService> _logger;
  private readonly IPinLookupHasher _pinLookupHasher;

  public MigrationService(AppDbContext context, ILogger<MigrationService> logger, IPinLookupHasher pinLookupHasher)
  {
    _context = context;
    _logger = logger;
    _pinLookupHasher = pinLookupHasher;
  }

  public async Task ExecuteAsync(CancellationToken cancellationToken = default)
  {
    try
    {
      _logger.LogInformation("Starting database migration process...");
      await _context.Database.MigrateAsync(cancellationToken);
      _logger.LogInformation("Database migration completed successfully.");

      _logger.LogInformation("Executing database seeders...");
      ISeeder[] seeders =
      [
          new RoleSeeder(),
          new ResourcePermissionSeeder(),
          new RolePermissionSeeder(),
          new StoreAndEmployeeSeeder(_pinLookupHasher),
          new MemberTierSeeder()
      ];
      foreach (var seeder in seeders)
      {
        await seeder.SeedAsync(_context, cancellationToken);
      }
      _logger.LogInformation("All seeders executed successfully.");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "An error occurred during database migration and seeding.");
      throw;
    }
  }
}