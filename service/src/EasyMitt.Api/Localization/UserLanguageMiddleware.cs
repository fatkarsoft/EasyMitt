using System.Globalization;
using EasyMitt.Domain.Localization;

namespace EasyMitt.Api.Localization;

public sealed class UserLanguageMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var language = context.User.FindFirst("language")?.Value;
        if (string.IsNullOrWhiteSpace(language))
        {
            language = ResolveFromAcceptLanguage(context.Request.Headers.AcceptLanguage.ToString());
        }

        language = SupportedLanguages.NormalizeOrDefault(language);
        var culture = CultureInfo.GetCultureInfo(language);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        await next(context);
    }

    private static string? ResolveFromAcceptLanguage(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        return header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split(';', 2)[0])
            .FirstOrDefault();
    }
}
