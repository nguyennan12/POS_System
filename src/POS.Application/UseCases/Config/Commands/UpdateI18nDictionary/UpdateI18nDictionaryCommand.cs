using POS.Application.Abstractions.Auth;
using POS.Application.Abstractions.Messaging;
using POS.Contracts.V1.Config;

namespace POS.Application.UseCases.Config.Commands.UpdateI18nDictionary;

public record UpdateI18nDictionaryCommand(string LanguageCode, Dictionary<string, string> Translations)
    : ICommand<I18nDictionaryResponse>, IRequirePermission
{
    public string RequiredPermission => "stores:update";
}
