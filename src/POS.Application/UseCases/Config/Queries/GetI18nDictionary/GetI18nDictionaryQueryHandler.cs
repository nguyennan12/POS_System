using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using POS.Application.Abstractions.Messaging;
using POS.Application.Abstractions.Persistence;
using POS.Contracts.V1.Config;
using POS.Domain.Common;

namespace POS.Application.UseCases.Config.Queries.GetI18nDictionary;

public class GetI18nDictionaryQueryHandler(ITranslationRepository translations)
    : IQueryHandler<GetI18nDictionaryQuery, I18nDictionaryResponse>
{
    public async Task<Result<I18nDictionaryResponse>> Handle(
        GetI18nDictionaryQuery query, CancellationToken cancellationToken)
    {
        var dict = query.LanguageCode.Equals("en", StringComparison.OrdinalIgnoreCase)
            ? I18nDefaults.GetEnglish()
            : I18nDefaults.GetVietnamese();

        var list = await translations.GetByLanguageAsync(query.LanguageCode, cancellationToken);
        foreach (var item in list)
        {
            dict[item.Key] = item.Value;
        }

        return new I18nDictionaryResponse(query.LanguageCode, dict);
    }
}
