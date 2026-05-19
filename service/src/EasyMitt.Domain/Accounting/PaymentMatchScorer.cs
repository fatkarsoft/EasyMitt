namespace EasyMitt.Domain.Accounting;

public sealed record PaymentMatchInput(
    decimal TransactionAmount,
    string TransactionDescription,
    string? TransactionCounterpartyName,
    string? TransactionCounterpartyIban,
    DateOnly TransactionBookingDate,
    decimal InvoiceOpenAmount,
    string? InvoiceNumber,
    string? InvoiceBuyerName,
    string? InvoiceBuyerIban,
    DateOnly? InvoiceIssueDate);

public sealed record PaymentMatchScore(
    decimal Confidence,
    int Score,
    IReadOnlyList<string> Reasons);

/// <summary>
/// Bank-tx → invoice match scorer. Confidence is a weighted blend in [0,1]:
///   amount-exact:    0.60
///   IBAN-match:      0.20
///   name-fuzzy:      0.10
///   date-proximity:  0.10
/// </summary>
public static class PaymentMatchScorer
{
    public const decimal AutoPreselectThreshold = 0.85m;

    public static PaymentMatchScore Score(PaymentMatchInput input)
    {
        var reasons = new List<string>();
        decimal confidence = 0m;
        var legacyScore = 0;

        var txAbs = Math.Abs(input.TransactionAmount);
        var open = input.InvoiceOpenAmount;
        if (open > 0)
        {
            var diff = Math.Abs(txAbs - open);
            if (diff <= 0.01m)
            {
                confidence += 0.6m;
                legacyScore += 55;
                reasons.Add("amount_exact");
            }
            else
            {
                var relative = diff / Math.Max(open, 0.01m);
                if (relative <= 0.02m)
                {
                    confidence += 0.5m;
                    legacyScore += 40;
                    reasons.Add("amount_near");
                }
                else if (txAbs < open)
                {
                    confidence += 0.15m;
                    legacyScore += 10;
                    reasons.Add("partial_amount");
                }
            }
        }

        var iban = (input.TransactionCounterpartyIban ?? "").Replace(" ", "").ToUpperInvariant();
        var invoiceIban = (input.InvoiceBuyerIban ?? "").Replace(" ", "").ToUpperInvariant();
        if (!string.IsNullOrEmpty(iban) && !string.IsNullOrEmpty(invoiceIban) && iban == invoiceIban)
        {
            confidence += 0.2m;
            legacyScore += 15;
            reasons.Add("iban_match");
        }

        var description = (input.TransactionDescription ?? "").ToLowerInvariant();
        var counterparty = (input.TransactionCounterpartyName ?? "").ToLowerInvariant();
        var invoiceNumber = (input.InvoiceNumber ?? "").Trim();
        if (invoiceNumber.Length > 0 && description.Contains(invoiceNumber.ToLowerInvariant()))
        {
            confidence += 0.1m;
            legacyScore += 25;
            reasons.Add("invoice_number");
        }

        var buyer = (input.InvoiceBuyerName ?? "").ToLowerInvariant().Trim();
        if (buyer.Length > 2)
        {
            var nameHit = description.Contains(buyer) || counterparty.Contains(buyer);
            if (!nameHit && buyer.Contains(' '))
            {
                var firstToken = buyer.Split(' ')[0];
                if (firstToken.Length >= 3 && (description.Contains(firstToken) || counterparty.Contains(firstToken)))
                {
                    nameHit = true;
                }
            }

            if (nameHit)
            {
                confidence += 0.1m;
                legacyScore += 15;
                reasons.Add("buyer_name");
            }
        }

        if (input.InvoiceIssueDate.HasValue)
        {
            var days = Math.Abs(input.TransactionBookingDate.DayNumber - input.InvoiceIssueDate.Value.DayNumber);
            if (days <= 60)
            {
                var dateScore = (60m - Math.Min(60m, days)) / 60m * 0.1m;
                if (dateScore > 0)
                {
                    confidence += dateScore;
                    reasons.Add("date_close");
                }
            }
        }

        if (confidence > 1m) confidence = 1m;
        if (confidence < 0m) confidence = 0m;

        return new PaymentMatchScore(
            decimal.Round(confidence, 2),
            Math.Min(100, legacyScore),
            reasons);
    }
}
