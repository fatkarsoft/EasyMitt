using EasyMitt.Application.Dtos.Ai;

namespace EasyMitt.Application.Abstractions.Ai;

public interface IMissingFieldSuggester
{
    Task<IReadOnlyList<InvoiceFieldSuggestionDto>> SuggestAsync(
        Guid companyId,
        Guid invoiceDraftId,
        IReadOnlyList<string> riskCodes,
        CancellationToken cancellationToken);
}
