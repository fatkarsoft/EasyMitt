namespace EasyMitt.Domain.Localization;

public static class SupportedLanguages
{
    public const string English = "en";
    public const string Turkish = "tr";
    public const string German = "de";

    public static readonly string[] All = [English, Turkish, German];

    public static string NormalizeOrDefault(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return English;
        }

        var normalized = language.Trim().ToLowerInvariant();
        if (normalized.Length > 2)
        {
            normalized = normalized[..2];
        }

        return All.Contains(normalized) ? normalized : English;
    }
}
