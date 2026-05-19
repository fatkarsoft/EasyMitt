using System.Globalization;
using System.Text.Json;
using EasyMitt.Application.Abstractions.Ai;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Ai;
using EasyMitt.Domain.Accounting;
using EasyMitt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Ai;

public sealed class DatevAccountSuggester(EasyMittDbContext db, IDatevSettingsRepository settingsRepository)
    : IDatevAccountSuggester
{
    public async Task<DatevAccountSuggestionDto?> SuggestAsync(
        Guid companyId,
        string documentType,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(companyId, cancellationToken);
        var expenseMappings = settings.ExpenseAccountMappings
            .Select(m => new DatevAccountMapping(m.Category, m.Account))
            .ToArray();
        var taxKeyMappings = settings.TaxKeyMappings
            .Select(m => new DatevTaxKeyMapping(m.Source, m.VatRate, m.TaxKey))
            .ToArray();

        if (string.Equals(documentType, "Expense", StringComparison.OrdinalIgnoreCase))
        {
            var expense = await db.Expenses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == documentId, cancellationToken);
            if (expense is null) return null;

            var vatRate = expense.NetAmount > 0
                ? decimal.Round(expense.TaxAmount / expense.NetAmount * 100m, 0)
                : 0m;
            var result = DatevAccountHeuristics.Suggest(new DatevAccountInput(
                "Expense",
                expense.Category,
                vatRate,
                expenseMappings,
                taxKeyMappings,
                settings.DefaultExpenseAccount,
                settings.RevenueAccount));

            return new DatevAccountSuggestionDto
            {
                DocumentType = "Expense",
                DocumentId = expense.Id,
                Account = result.Account,
                TaxKey = result.TaxKey,
                VatRate = vatRate,
                Confidence = result.Confidence,
                Rationale = result.Rationale,
                MatchedRule = result.MatchedRule,
            };
        }

        if (string.Equals(documentType, "Invoice", StringComparison.OrdinalIgnoreCase))
        {
            var invoice = await db.InvoiceDrafts.AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == documentId, cancellationToken);
            if (invoice is null) return null;

            var vatRate = ExtractInvoiceVatRate(invoice.PayloadJson);
            var result = DatevAccountHeuristics.Suggest(new DatevAccountInput(
                "Invoice",
                "",
                vatRate,
                expenseMappings,
                taxKeyMappings,
                settings.DefaultExpenseAccount,
                settings.RevenueAccount));

            return new DatevAccountSuggestionDto
            {
                DocumentType = "Invoice",
                DocumentId = invoice.Id,
                Account = result.Account,
                TaxKey = result.TaxKey,
                VatRate = vatRate,
                Confidence = result.Confidence,
                Rationale = result.Rationale,
                MatchedRule = result.MatchedRule,
            };
        }

        return null;
    }

    private static decimal ExtractInvoiceVatRate(string payloadJson)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            if (!document.RootElement.TryGetProperty("lines", out var lines) || lines.ValueKind != JsonValueKind.Array)
                return 19m;

            decimal? rate = null;
            foreach (var line in lines.EnumerateArray())
            {
                if (line.TryGetProperty("BT-152", out var rateElement))
                {
                    if (rateElement.ValueKind == JsonValueKind.Number && rateElement.TryGetDecimal(out var num))
                    {
                        rate = decimal.Round(num, 0);
                        break;
                    }
                    if (rateElement.ValueKind == JsonValueKind.String &&
                        decimal.TryParse(rateElement.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                    {
                        rate = decimal.Round(parsed, 0);
                        break;
                    }
                }
            }
            return rate ?? 19m;
        }
        catch (JsonException)
        {
            return 19m;
        }
    }
}
