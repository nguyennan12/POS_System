using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Contracts.V1.Config;
using POS.Domain.Common;
using POS.Domain.Configuration;

namespace POS.Application.UseCases.Config.Commands.UpdateI18nDictionary;

public class UpdateI18nDictionaryCommandHandler(
    ITranslationRepository translations,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateI18nDictionaryCommand, I18nDictionaryResponse>
{
    public async Task<Result<I18nDictionaryResponse>> Handle(
        UpdateI18nDictionaryCommand command, CancellationToken cancellationToken)
    {
        var existing = await translations.GetByLanguageAsync(command.LanguageCode, cancellationToken);
        var existingMap = existing.ToDictionary(t => t.Key);

        foreach (var (key, value) in command.Translations)
        {
            if (existingMap.TryGetValue(key, out var translation))
                translation.SetValue(value);
            else
                await translations.AddAsync(
                    Translation.Create(command.LanguageCode, key, value), cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await translations.GetByLanguageAsync(command.LanguageCode, cancellationToken);
        var dict = updated.ToDictionary(t => t.Key, t => t.Value);
        return new I18nDictionaryResponse(command.LanguageCode, dict);
    }
}
