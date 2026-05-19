namespace EasyMitt.Domain.Accounting;

public static class ExpenseCategories
{
    public const string General = "General";
    public const string OfficeSupplies = "OfficeSupplies";
    public const string Travel = "Travel";
    public const string Meals = "Meals";
    public const string Software = "Software";
    public const string Telecom = "Telecom";
    public const string Utilities = "Utilities";
    public const string Rent = "Rent";
    public const string ProfessionalServices = "ProfessionalServices";
    public const string Marketing = "Marketing";
    public const string Fuel = "Fuel";
    public const string Insurance = "Insurance";

    public static readonly IReadOnlyList<string> All = new[]
    {
        General,
        OfficeSupplies,
        Travel,
        Meals,
        Software,
        Telecom,
        Utilities,
        Rent,
        ProfessionalServices,
        Marketing,
        Fuel,
        Insurance,
    };
}

public sealed record ExpenseCategoryInput(
    string VendorName,
    string? LineDescriptions,
    decimal? TotalAmount,
    string? CurrencyCode);

public sealed record ExpenseCategoryResult(
    string Category,
    decimal Confidence,
    string Rationale,
    IReadOnlyList<string> Signals);

/// <summary>
/// Pure heuristic classifier — no IO. Future LLM-backed impl can replace
/// the calling service while keeping this as a fallback.
/// </summary>
public static class ExpenseCategoryHeuristics
{
    private static readonly (string Category, string[] Keywords, decimal Weight)[] Rules =
    {
        (ExpenseCategories.OfficeSupplies, new[] { "office", "büro", "papier", "stift", "tinte", "drucker", "toner", "staples", "kalem", "kağıt", "ofis" }, 0.85m),
        (ExpenseCategories.Travel, new[] { "hotel", "flight", "flug", "lufthansa", "db ", "deutsche bahn", "bahn ", "train", "uber", "taxi", "rental", "mietwagen", "otel", "ucak" }, 0.88m),
        (ExpenseCategories.Meals, new[] { "restaurant", "cafe", "café", "lunch", "dinner", "mensa", "imbiss", "lokanta", "yemek", "kahve", "starbucks" }, 0.86m),
        (ExpenseCategories.Software, new[] { "saas", "subscription", "license", "lizenz", "microsoft", "github", "adobe", "jetbrains", "atlassian", "google workspace", "openai", "anthropic", "yazılım", "abonelik" }, 0.92m),
        (ExpenseCategories.Telecom, new[] { "telekom", "vodafone", "o2", "1&1", "mobile", "mobilfunk", "telefon", "internet", "telefoni", "iletişim" }, 0.88m),
        (ExpenseCategories.Utilities, new[] { "strom", "gas", "wasser", "electricity", "energy", "stadtwerke", "elektrik", "su faturası" }, 0.86m),
        (ExpenseCategories.Rent, new[] { "miete", "rent", "vermieter", "kiraci", "kira", "lease" }, 0.9m),
        (ExpenseCategories.ProfessionalServices, new[] { "consulting", "beratung", "anwalt", "lawyer", "notar", "steuerberater", "accountant", "danışmanlık", "müşavir" }, 0.84m),
        (ExpenseCategories.Marketing, new[] { "marketing", "werbung", "ads", "google ads", "meta ads", "facebook ads", "instagram", "linkedin ads", "kampagne", "reklam" }, 0.82m),
        (ExpenseCategories.Fuel, new[] { "shell", "aral", "esso", "total", "tankstelle", "fuel", "diesel", "benzin", "petrol", "akaryakit" }, 0.92m),
        (ExpenseCategories.Insurance, new[] { "versicherung", "insurance", "allianz", "axa", "huk", "sigorta" }, 0.88m),
    };

    public static ExpenseCategoryResult Classify(ExpenseCategoryInput input)
    {
        var haystack = ((input.VendorName ?? "") + " " + (input.LineDescriptions ?? "")).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(haystack))
        {
            return new ExpenseCategoryResult(
                ExpenseCategories.General,
                0.2m,
                "no_signal",
                Array.Empty<string>());
        }

        string? bestCategory = null;
        decimal bestConfidence = 0m;
        var signals = new List<string>();

        foreach (var (category, keywords, weight) in Rules)
        {
            var hits = keywords.Where(k => haystack.Contains(k)).ToArray();
            if (hits.Length == 0) continue;

            // base weight + small bonus per extra hit, capped
            var confidence = Math.Min(0.98m, weight + (hits.Length - 1) * 0.03m);

            // amount sanity adjustments
            if (input.TotalAmount.HasValue)
            {
                if (category == ExpenseCategories.Meals && input.TotalAmount > 1000m) confidence -= 0.1m;
                if (category == ExpenseCategories.Rent && input.TotalAmount < 50m) confidence -= 0.15m;
            }

            if (confidence > bestConfidence)
            {
                bestConfidence = confidence;
                bestCategory = category;
                signals = hits.Select(h => $"keyword:{h}").ToList();
            }
        }

        if (bestCategory is null)
        {
            return new ExpenseCategoryResult(
                ExpenseCategories.General,
                0.25m,
                "no_keyword_match",
                Array.Empty<string>());
        }

        return new ExpenseCategoryResult(
            bestCategory,
            decimal.Round(Math.Max(0m, Math.Min(bestConfidence, 0.99m)), 2),
            "keyword_match",
            signals);
    }
}
