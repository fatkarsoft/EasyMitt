using System.Text.Json;
using EasyMitt.Application.Abstractions.Ai;
using EasyMitt.Application.Dtos.Ai;
using EasyMitt.Domain.Accounting;
using EasyMitt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Ai;

public sealed class MissingFieldSuggester(EasyMittDbContext db) : IMissingFieldSuggester
{
    public async Task<IReadOnlyList<InvoiceFieldSuggestionDto>> SuggestAsync(
        Guid companyId,
        Guid invoiceDraftId,
        IReadOnlyList<string> riskCodes,
        CancellationToken cancellationToken)
    {
        var invoice = await db.InvoiceDrafts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == invoiceDraftId, cancellationToken);
        if (invoice is null) return Array.Empty<InvoiceFieldSuggestionDto>();

        var input = new InvoiceFieldSuggestionInput(
            ReadString(invoice.PayloadJson, "buyer", "BT-48"),
            ReadString(invoice.PayloadJson, "buyer", "BT-10"),
            ReadString(invoice.PayloadJson, "seller", "BT-31"),
            ReadString(invoice.PayloadJson, "seller", "BT-34"),
            ReadString(invoice.PayloadJson, "core", "BT-5"),
            riskCodes ?? Array.Empty<string>());

        return InvoiceFieldHeuristics.Suggest(input)
            .Select(x => new InvoiceFieldSuggestionDto
            {
                InvoiceDraftId = invoiceDraftId,
                FieldCode = x.FieldCode,
                SuggestedValue = x.SuggestedValue,
                Rationale = x.Rationale,
                Confidence = x.Confidence,
            })
            .ToArray();
    }

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
}
