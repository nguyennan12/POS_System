namespace POS.Infrastructure.Persistence.Seeders;

public interface ISeeder
{
  Task SeedAsync(AppDbContext context, CancellationToken cancellationToken);
}