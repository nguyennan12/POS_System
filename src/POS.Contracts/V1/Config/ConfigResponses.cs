namespace POS.Contracts.V1.Config;

public record StoreConfigResponse(
    Guid? StoreId,
    IReadOnlyDictionary<string, string> Configs
);

public record I18nDictionaryResponse(
    string LanguageCode,
    IReadOnlyDictionary<string, string> Translations
);
