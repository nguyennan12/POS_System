using Microsoft.EntityFrameworkCore;
using POS.Application.Abstractions.Persistence;
using POS.Domain.Configuration;

namespace POS.Infrastructure.Persistence.Repositories;

public class TranslationRepository : ITranslationRepository
{
    private readonly AppDbContext _context;

    public TranslationRepository(AppDbContext context) => _context = context;

    public Task<List<Translation>> GetByLanguageAsync(string languageCode, CancellationToken cancellationToken = default) =>
        _context.Translations
            .AsNoTracking()
            .Where(t => t.LanguageCode == languageCode)
            .ToListAsync(cancellationToken);

    public Task<Translation?> GetAsync(string languageCode, string key, CancellationToken cancellationToken = default) =>
        _context.Translations
            .FirstOrDefaultAsync(t => t.LanguageCode == languageCode && t.Key == key, cancellationToken);

    public async Task AddAsync(Translation translation, CancellationToken cancellationToken = default) =>
        await _context.Translations.AddAsync(translation, cancellationToken);

    public void Remove(Translation translation) =>
        _context.Translations.Remove(translation);
}
