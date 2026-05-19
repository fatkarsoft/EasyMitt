using EasyMitt.Application.Dtos.Ai;
using EasyMitt.Application.Dtos.Ingestion;

namespace EasyMitt.Application.Abstractions.Ai;

public interface IExpenseCategorySuggester
{
    ExpenseCategorySuggestionDto SuggestFromScan(RawInvoiceImportDto raw);

    ExpenseCategorySuggestionDto SuggestFromFields(string vendorName, string? lineDescriptions, decimal? totalAmount, string? currencyCode);
}
