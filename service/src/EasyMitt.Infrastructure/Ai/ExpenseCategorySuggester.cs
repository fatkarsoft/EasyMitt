using EasyMitt.Application.Abstractions.Ai;
using EasyMitt.Application.Dtos.Ai;
using EasyMitt.Application.Dtos.Ingestion;
using EasyMitt.Domain.Accounting;

namespace EasyMitt.Infrastructure.Ai;

public sealed class ExpenseCategorySuggester : IExpenseCategorySuggester
{
    public ExpenseCategorySuggestionDto SuggestFromScan(RawInvoiceImportDto raw)
    {
        var descriptions = raw.LineHints.Count == 0
            ? null
            : string.Join("; ", raw.LineHints.Select(x => x.Description).Where(x => !string.IsNullOrWhiteSpace(x)));
        return SuggestFromFields(
            raw.MerchantOrSellerHint ?? "",
            descriptions,
            raw.TotalAmount,
            raw.CurrencyHint);
    }

    public ExpenseCategorySuggestionDto SuggestFromFields(string vendorName, string? lineDescriptions, decimal? totalAmount, string? currencyCode)
    {
        var result = ExpenseCategoryHeuristics.Classify(new ExpenseCategoryInput(
            vendorName ?? "",
            lineDescriptions,
            totalAmount,
            currencyCode));
        return new ExpenseCategorySuggestionDto
        {
            Category = result.Category,
            Confidence = result.Confidence,
            Rationale = result.Rationale,
            Signals = result.Signals,
        };
    }
}
