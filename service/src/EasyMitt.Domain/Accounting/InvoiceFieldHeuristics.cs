namespace EasyMitt.Domain.Accounting;

public sealed record InvoiceFieldSuggestionInput(
    string? BuyerVatId,
    string? BuyerReference,
    string? SellerVatId,
    string? SellerIban,
    string? CurrencyCode,
    IReadOnlyList<string> RiskCodes);

public sealed record InvoiceFieldSuggestion(
    string FieldCode,
    string SuggestedValue,
    string Rationale,
    decimal Confidence);

/// <summary>
/// Reads compliance risks and computed BT-field values to produce
/// non-destructive suggested fixes (e.g. value looks like Leitweg-ID,
/// move it from BT-48 (Buyer VAT) into BT-10 (Buyer reference)).
/// </summary>
public static class InvoiceFieldHeuristics
{
    public static IReadOnlyList<InvoiceFieldSuggestion> Suggest(InvoiceFieldSuggestionInput input)
    {
        var suggestions = new List<InvoiceFieldSuggestion>();

        if (!string.IsNullOrWhiteSpace(input.BuyerVatId))
        {
            var raw = input.BuyerVatId.Trim();
            // Leitweg-ID typically starts with a numeric prefix like "991-..." (B2G)
            if (LooksLikeLeitwegId(raw))
            {
                suggestions.Add(new InvoiceFieldSuggestion(
                    "BT-10",
                    raw,
                    "buyer_vat_looks_like_leitweg",
                    0.92m));
            }
        }

        if (input.RiskCodes.Contains("missing_seller_iban") && !string.IsNullOrWhiteSpace(input.SellerIban) && input.SellerIban.Trim().Length < 15)
        {
            suggestions.Add(new InvoiceFieldSuggestion(
                "BT-34",
                input.SellerIban.Trim(),
                "iban_too_short",
                0.7m));
        }

        if (input.RiskCodes.Contains("missing_seller_vat") && !string.IsNullOrWhiteSpace(input.SellerVatId))
        {
            suggestions.Add(new InvoiceFieldSuggestion(
                "BT-31",
                input.SellerVatId.Trim(),
                "seller_vat_present_but_unset",
                0.6m));
        }

        return suggestions;
    }

    private static bool LooksLikeLeitwegId(string value)
    {
        if (value.Length < 4) return false;
        if (value.StartsWith("991-", StringComparison.Ordinal)) return true;
        if (value.StartsWith("992-", StringComparison.Ordinal)) return true;
        if (value.StartsWith("993-", StringComparison.Ordinal)) return true;
        // 12-digit numeric-only buyer references are also typical Leitweg-IDs
        var stripped = new string(value.Where(c => char.IsDigit(c) || c == '-').ToArray());
        return stripped.Length >= 8 && stripped.Contains('-') && value.IndexOfAny(new[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' }) < 0;
    }
}
