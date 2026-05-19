namespace EasyMitt.Domain.Accounting;

public sealed record DatevAccountMapping(string Category, string Account);
public sealed record DatevTaxKeyMapping(string Source, decimal VatRate, string TaxKey);

public sealed record DatevAccountInput(
    string DocumentType,
    string Category,
    decimal VatRate,
    IReadOnlyList<DatevAccountMapping> ExpenseMappings,
    IReadOnlyList<DatevTaxKeyMapping> TaxKeyMappings,
    string DefaultExpenseAccount,
    string RevenueAccount);

public sealed record DatevAccountResult(
    string Account,
    string TaxKey,
    decimal Confidence,
    string MatchedRule,
    string Rationale);

public static class DatevAccountHeuristics
{
    public static DatevAccountResult Suggest(DatevAccountInput input)
    {
        var isInvoice = string.Equals(input.DocumentType, "Invoice", StringComparison.OrdinalIgnoreCase);
        var source = isInvoice ? "Invoice" : "Expense";

        var taxKey = input.TaxKeyMappings
            .FirstOrDefault(m => string.Equals(m.Source, source, StringComparison.OrdinalIgnoreCase) && m.VatRate == input.VatRate)?.TaxKey ?? "";

        if (isInvoice)
        {
            var account = string.IsNullOrWhiteSpace(input.RevenueAccount) ? "8400" : input.RevenueAccount;
            return new DatevAccountResult(
                account,
                taxKey,
                string.IsNullOrEmpty(taxKey) ? 0.6m : 0.9m,
                "settings.revenue_account",
                string.IsNullOrEmpty(taxKey)
                    ? "revenue_account_default_no_taxkey"
                    : "revenue_account_default_with_taxkey");
        }

        var mapped = input.ExpenseMappings
            .FirstOrDefault(m => string.Equals(m.Category, input.Category, StringComparison.OrdinalIgnoreCase));
        if (mapped is not null && !string.IsNullOrWhiteSpace(mapped.Account))
        {
            return new DatevAccountResult(
                mapped.Account.Trim(),
                taxKey,
                string.IsNullOrEmpty(taxKey) ? 0.85m : 0.95m,
                "settings.expense_mapping",
                "category_mapped");
        }

        var fallback = string.IsNullOrWhiteSpace(input.DefaultExpenseAccount) ? "4980" : input.DefaultExpenseAccount;
        return new DatevAccountResult(
            fallback,
            taxKey,
            string.IsNullOrEmpty(taxKey) ? 0.35m : 0.55m,
            "settings.default_expense_account",
            "fallback_default");
    }
}
