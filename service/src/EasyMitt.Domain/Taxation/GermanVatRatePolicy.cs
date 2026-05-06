namespace EasyMitt.Domain.Taxation;

public static class GermanVatRatePolicy
{
    private static readonly decimal[] CommonRates = [0m, 7m, 19m];

    public static bool IsCommonRate(decimal rate) => CommonRates.Contains(rate);

    public static decimal NormalizeOrDefault(decimal? rate) =>
        rate is { } value && IsCommonRate(value) ? value : 19m;
}
