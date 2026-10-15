using POS.Application.Abstractions.Messaging;
using POS.Contracts.V1.Config;

namespace POS.Application.UseCases.Config.Queries.GetI18nDictionary;

public record GetI18nDictionaryQuery(string LanguageCode) : IQuery<I18nDictionaryResponse>;
