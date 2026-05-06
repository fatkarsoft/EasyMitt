using System.Globalization;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Domain.Localization;

namespace EasyMitt.Api.Localization;

public sealed class HttpCurrentLanguage(IHttpContextAccessor httpContextAccessor) : ICurrentLanguage
{
    public string Language
    {
        get
        {
            var claimLanguage = httpContextAccessor.HttpContext?.User.FindFirst("language")?.Value;
            if (!string.IsNullOrWhiteSpace(claimLanguage))
            {
                return SupportedLanguages.NormalizeOrDefault(claimLanguage);
            }

            return SupportedLanguages.NormalizeOrDefault(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        }
    }
}
