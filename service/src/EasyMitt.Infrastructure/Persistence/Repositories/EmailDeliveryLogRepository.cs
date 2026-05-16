using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Email;
using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Persistence.Repositories;

public sealed class EmailDeliveryLogRepository(EasyMittDbContext db) : IEmailDeliveryLogRepository
{
    public async Task AddAsync(EmailDeliveryLogEntry entry, CancellationToken ct = default)
    {
        var entity = new EmailDeliveryLogEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = entry.CompanyId,
            DocumentType = entry.DocumentType,
            DocumentId = entry.DocumentId,
            ToEmail = entry.ToEmail,
            Subject = entry.Subject,
            AttachmentType = entry.AttachmentType,
            Status = entry.Status,
            ErrorMessage = entry.ErrorMessage,
            SenderUserId = entry.SenderUserId,
            SenderUserEmail = entry.SenderUserEmail,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.EmailDeliveryLogs.Add(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<EmailDeliveryLogDto>> GetByDocumentAsync(
        Guid companyId, string documentType, Guid documentId, CancellationToken ct = default)
    {
        return await db.EmailDeliveryLogs.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.DocumentType == documentType && x.DocumentId == documentId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new EmailDeliveryLogDto(
                x.Id,
                x.DocumentType,
                x.DocumentId,
                x.ToEmail,
                x.Subject,
                x.AttachmentType,
                x.Status,
                x.ErrorMessage,
                x.SenderUserEmail,
                x.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EmailDeliveryLogDto>> GetRecentAsync(
        Guid companyId, int limit, CancellationToken ct = default)
    {
        return await db.EmailDeliveryLogs.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(limit)
            .Select(x => new EmailDeliveryLogDto(
                x.Id,
                x.DocumentType,
                x.DocumentId,
                x.ToEmail,
                x.Subject,
                x.AttachmentType,
                x.Status,
                x.ErrorMessage,
                x.SenderUserEmail,
                x.CreatedAtUtc))
            .ToListAsync(ct);
    }
}
