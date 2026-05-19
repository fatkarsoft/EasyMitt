namespace EasyMitt.Domain.Compliance;

/// <summary>
/// KoSIT XRechnung Schematron alt kümesi (BR-* + BR-DE-*).
/// Saf, DB'siz, deterministik. Infrastructure tarafından argüman olarak çağrılır.
/// </summary>
public static class XRechnungSchematronRules
{
    public sealed record RuleInput(
        string InvoiceNumber,
        DateOnly IssueDate,
        string CurrencyCode,
        string BuyerReference,
        decimal TaxAmount,
        decimal InvoiceTotalVatIncluded,
        string SellerName,
        string? SellerVatId,
        string? SellerIban,
        string BuyerName,
        string? BuyerVatId,
        int LineCount,
        decimal LinesNetSum,
        decimal MaxLineVatRatePercent);

    public sealed record RuleFailure(string RuleId, string Severity, string Description, string? Field);

    public static IReadOnlyList<RuleFailure> Evaluate(RuleInput input)
    {
        var failures = new List<RuleFailure>();

        // BR-01: Invoice must have a Specification identifier (uygulamamızda XRechnung profili sabit — atlanır).

        // BR-02: Invoice must have an InvoiceNumber.
        if (string.IsNullOrWhiteSpace(input.InvoiceNumber))
            failures.Add(new RuleFailure("BR-02", "fatal", "An Invoice shall have an Invoice number (BT-1).", "BT-1"));

        // BR-03: Invoice issue date.
        if (input.IssueDate == default)
            failures.Add(new RuleFailure("BR-03", "fatal", "An Invoice shall have an Invoice issue date (BT-2).", "BT-2"));

        // BR-04: Invoice type code — kullanılan sabit profilden geliyor; atlanır.

        // BR-05: Invoice currency code.
        if (string.IsNullOrWhiteSpace(input.CurrencyCode))
            failures.Add(new RuleFailure("BR-05", "fatal", "An Invoice shall have an Invoice currency code (BT-5).", "BT-5"));

        // BR-06: Seller name.
        if (string.IsNullOrWhiteSpace(input.SellerName))
            failures.Add(new RuleFailure("BR-06", "fatal", "An Invoice shall contain the Seller name (BT-27/BT-20).", "BT-20"));

        // BR-07: Buyer name.
        if (string.IsNullOrWhiteSpace(input.BuyerName))
            failures.Add(new RuleFailure("BR-07", "fatal", "An Invoice shall contain the Buyer name (BT-44/BT-26).", "BT-26"));

        // BR-16: At least one invoice line.
        if (input.LineCount <= 0)
            failures.Add(new RuleFailure("BR-16", "fatal", "An Invoice shall have at least one Invoice line (BG-25).", "Lines"));

        // BR-CO-15: Total amount = sum of line net amounts + tax (approx).
        var expectedTotal = decimal.Round(input.LinesNetSum + input.TaxAmount, 2);
        var actualTotal = decimal.Round(input.InvoiceTotalVatIncluded, 2);
        if (input.LineCount > 0 && Math.Abs(expectedTotal - actualTotal) > 0.05m)
            failures.Add(new RuleFailure("BR-CO-15", "fatal", "Invoice total amount with VAT (BT-112) must equal sum of line net amounts plus tax (BT-110).", "BT-112"));

        // BR-DE-01: Buyer reference (Leitweg-ID for B2G) required.
        if (string.IsNullOrWhiteSpace(input.BuyerReference))
            failures.Add(new RuleFailure("BR-DE-01", "fatal", "Buyer reference (BT-10) is mandatory for German XRechnung.", "BT-10"));

        // BR-DE-15: Seller VAT identifier or German tax registration number.
        if (string.IsNullOrWhiteSpace(input.SellerVatId))
            failures.Add(new RuleFailure("BR-DE-15", "warning", "Seller VAT identifier (BT-31) or German tax registration (BT-32) is expected.", "BT-22"));

        // BR-DE-16: Payment IBAN required when payment means is credit transfer.
        if (string.IsNullOrWhiteSpace(input.SellerIban))
            failures.Add(new RuleFailure("BR-DE-16", "warning", "Payment account identifier (BT-84/BT-34) is expected for credit transfer.", "BT-34"));

        // BR-DE-21: Currency must be EUR for German B2G.
        if (!string.Equals(input.CurrencyCode, "EUR", StringComparison.OrdinalIgnoreCase))
            failures.Add(new RuleFailure("BR-DE-21", "warning", "Currency code (BT-5) should be EUR for German invoices.", "BT-5"));

        // BR-DE-23: VAT rate range plausibility (0 / 7 / 19).
        if (input.MaxLineVatRatePercent > 0 && input.MaxLineVatRatePercent != 7 && input.MaxLineVatRatePercent != 19)
            failures.Add(new RuleFailure("BR-DE-23", "warning", "VAT rate (BT-152) should be one of 0, 7 or 19 for Germany.", "BT-151"));

        // BR-DE-26: BuyerReference looks like Leitweg-ID (heuristic: digits & dashes).
        if (!string.IsNullOrWhiteSpace(input.BuyerReference)
            && !LooksLikeLeitwegId(input.BuyerReference))
            failures.Add(new RuleFailure("BR-DE-26", "info", "Buyer reference (BT-10) does not look like a Leitweg-ID.", "BT-10"));

        return failures;
    }

    private static bool LooksLikeLeitwegId(string value)
    {
        // Leitweg-ID kabaca: 04011000-12345-67 gibi rakam+tire.
        var digits = 0;
        foreach (var c in value)
        {
            if (char.IsDigit(c)) digits++;
            else if (c is '-' or '_') continue;
            else return false;
        }
        return digits >= 4;
    }
}
