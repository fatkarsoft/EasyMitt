using EasyMitt.Application.Dtos.Ai;

namespace EasyMitt.Application.Abstractions.Persistence;

public interface IAiSuggestionRepository
{
    Task<AiSuggestionDto> RecordAsync(
        Guid companyId,
        AiSuggestionCreateRequest request,
        string status,
        string? decidedByUserEmail,
        CancellationToken cancellationToken);

    Task<AiSuggestionDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AiSuggestionDto>> SearchAsync(
        Guid companyId,
        string? suggestionType,
        string? status,
        string? targetType,
        Guid? targetId,
        int take,
        CancellationToken cancellationToken);

    Task<AiSuggestionDto?> DecideAsync(
        Guid companyId,
        Guid id,
        string newStatus,
        string decidedByUserEmail,
        CancellationToken cancellationToken);

    Task<AiSuggestionDto?> SupersedeAsync(
        Guid companyId,
        Guid id,
        Guid newSuggestionId,
        string decidedByUserEmail,
        CancellationToken cancellationToken);

    Task<AiSuggestionDto?> FindLatestPendingAsync(
        Guid companyId,
        string suggestionType,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken);
}
