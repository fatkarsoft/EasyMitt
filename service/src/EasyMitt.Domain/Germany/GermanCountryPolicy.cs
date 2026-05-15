namespace EasyMitt.Domain.Germany;

public static class GermanCountryPolicy
{
    public const string CountryCode = "DE";

    public static string NormalizeCountryCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? CountryCode : value.Trim().ToUpperInvariant();
}
