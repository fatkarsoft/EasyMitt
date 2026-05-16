using EasyMitt.Application.Dtos.Email;

namespace EasyMitt.Application.Abstractions.Persistence;

public interface IEmailDeliveryLogRepository
{
    Task AddAsync(EmailDeliveryLogEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<EmailDeliveryLogDto>> GetByDocumentAsync(Guid companyId, string documentType, Guid documentId, CancellationToken ct = default);
    Task<IReadOnlyList<EmailDeliveryLogDto>> GetRecentAsync(Guid companyId, int limit, CancellationToken ct = default);
}

public sealed record EmailDeliveryLogEntry(
    Guid CompanyId,
    string DocumentType,
    Guid DocumentId,
    string ToEmail,
    string Subject,
    string AttachmentType,
    string Status,
    string? ErrorMessage,
    string SenderUserId,
    string SenderUserEmail);
