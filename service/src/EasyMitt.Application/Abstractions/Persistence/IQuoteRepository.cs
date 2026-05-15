using EasyMitt.Application.Dtos.Quotes;

namespace EasyMitt.Application.Abstractions.Persistence;

public interface IQuoteRepository
{
    Task<IReadOnlyList<QuoteDto>> SearchAsync(Guid companyId, string? query, string? status, CancellationToken cancellationToken);
    Task<QuoteDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken);
    Task<QuoteDto> CreateAsync(Guid companyId, QuoteUpsertDto request, CancellationToken cancellationToken);
    Task<QuoteDto?> UpdateAsync(Guid companyId, Guid id, QuoteUpsertDto request, CancellationToken cancellationToken);
    Task<QuoteDto?> UpdateStatusAsync(Guid companyId, Guid id, string status, DateTime nowUtc, Guid? convertedInvoiceDraftId, CancellationToken cancellationToken);
}
