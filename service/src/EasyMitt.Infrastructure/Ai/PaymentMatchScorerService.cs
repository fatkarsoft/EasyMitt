using System.Globalization;
using System.Text.Json;
using EasyMitt.Application.Abstractions.Ai;
using EasyMitt.Application.Dtos.Ai;
using EasyMitt.Domain.Accounting;
using EasyMitt.Domain.Billing;
using EasyMitt.Infrastructure.Persistence;
using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Ai;

public sealed class PaymentMatchScorerService(EasyMittDbContext db) : IPaymentMatchScorer
{
    public async Task<IReadOnlyList<PaymentMatchSuggestionDto>> SuggestAsync(
        Guid companyId,
        Guid bankTransactionId,
        CancellationToken cancellationToken)
    {
        var transaction = await db.BankTransactions.AsNoTracking()
            .Include(x => x.Allocations)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == bankTransactionId, cancellationToken);
        if (transaction is null) return Array.Empty<PaymentMatchSuggestionDto>();

        var invoices = await db.InvoiceDrafts.AsNoTracking()
            .Where(x => x.CompanyId == companyId
                && x.Status != InvoiceLifecycleStatus.Draft
                && x.Status != InvoiceLifecycleStatus.Cancelled
                && x.Status != InvoiceLifecycleStatus.Paid)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        var invoiceIds = invoices.Select(x => x.Id).ToArray();
        var paidByInvoice = await db.PaymentAllocations.AsNoTracking()
            .Where(x => x.CompanyId == companyId && invoiceIds.Contains(x.InvoiceDraftId))
            .GroupBy(x => x.InvoiceDraftId)
            .Select(g => new { g.Key, Amount = g.Sum(y => y.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Amount, cancellationToken);

        var results = new List<PaymentMatchSuggestionDto>();
        foreach (var invoice in invoices)
        {
            var paid = paidByInvoice.GetValueOrDefault(invoice.Id);
            var total = ReadDecimal(invoice.PayloadJson, "core", "BT-112");
            var open = Math.Max(0, total - paid);
            if (open <= 0) continue;

            var input = new PaymentMatchInput(
                transaction.Amount,
                transaction.Description,
                transaction.CounterpartyName,
                transaction.CounterpartyIban,
                transaction.BookingDate,
                open,
                ReadString(invoice.PayloadJson, "core", "BT-1"),
                ReadString(invoice.PayloadJson, "buyer", "BT-26"),
                ReadString(invoice.PayloadJson, "buyer", "BT-49"),
                ParseDate(ReadString(invoice.PayloadJson, "core", "BT-2")));

            var score = PaymentMatchScorer.Score(input);

            results.Add(new PaymentMatchSuggestionDto
            {
                InvoiceDraftId = invoice.Id,
                InvoiceNumber = input.InvoiceNumber ?? "",
                BuyerName = input.InvoiceBuyerName ?? "",
                IssueDate = input.InvoiceIssueDate ?? DateOnly.FromDateTime(invoice.CreatedAtUtc),
                InvoiceTotal = total,
                PaidAmount = paid,
                OpenAmount = open,
                Confidence = score.Confidence,
                Score = score.Score,
                Reasons = score.Reasons,
                AutoPreselect = score.Confidence >= PaymentMatchScorer.AutoPreselectThreshold,
            });
        }

        var ordered = results
            .OrderByDescending(x => x.Confidence)
            .ThenBy(x => Math.Abs(x.OpenAmount - Math.Abs(transaction.Amount)))
            .Take(10)
            .ToArray();

        // Only preselect the single top suggestion if it's strongly above threshold
        if (ordered.Length > 1 && ordered[0].AutoPreselect)
        {
            var clamped = new PaymentMatchSuggestionDto[ordered.Length];
            clamped[0] = ordered[0];
            for (var i = 1; i < ordered.Length; i++)
            {
                var prev = ordered[i];
                clamped[i] = new PaymentMatchSuggestionDto
                {
                    InvoiceDraftId = prev.InvoiceDraftId,
                    InvoiceNumber = prev.InvoiceNumber,
                    BuyerName = prev.BuyerName,
                    IssueDate = prev.IssueDate,
                    InvoiceTotal = prev.InvoiceTotal,
                    PaidAmount = prev.PaidAmount,
                    OpenAmount = prev.OpenAmount,
                    Confidence = prev.Confidence,
                    Score = prev.Score,
                    Reasons = prev.Reasons,
                    AutoPreselect = false,
                };
            }
            return clamped;
        }

        return ordered;
    }

    private static DateOnly? ParseDate(string text) =>
        DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value) ? value : null;

    private static string ReadString(string payloadJson, string section, string property)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            return document.RootElement.TryGetProperty(section, out var sectionElement) &&
                sectionElement.TryGetProperty(property, out var value)
                    ? value.ToString()
                    : "";
        }
        catch (JsonException)
        {
            return "";
        }
    }

    private static decimal ReadDecimal(string payloadJson, string section, string property)
    {
        var text = ReadString(payloadJson, section, property);
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }
}
