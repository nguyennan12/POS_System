namespace POS.Contracts.V1.Config;

public record UpdateStoreConfigRequest(
    Dictionary<string, string> Configs
);

public record UpdateI18nDictionaryRequest(
    Dictionary<string, string> Translations
);
