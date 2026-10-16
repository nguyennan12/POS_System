using POS.Domain.Configuration;

namespace POS.Application.Abstractions.Persistence;

public interface ITranslationRepository
{
    Task<List<Translation>> GetByLanguageAsync(string languageCode, CancellationToken cancellationToken = default);
    Task<Translation?> GetAsync(string languageCode, string key, CancellationToken cancellationToken = default);
    Task AddAsync(Translation translation, CancellationToken cancellationToken = default);
    void Remove(Translation translation);
}
