using EasyMitt.Application.Dtos.Ai;

namespace EasyMitt.Application.Abstractions.Ai;

public interface IDatevAccountSuggester
{
    Task<DatevAccountSuggestionDto?> SuggestAsync(
        Guid companyId,
        string documentType,
        Guid documentId,
        CancellationToken cancellationToken);
}
