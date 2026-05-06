using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Persistence.Repositories;

public sealed class InvoiceDraftRepository(EasyMittDbContext db) : IInvoiceDraftRepository
{
    public async Task<Guid> InsertAsync(InvoiceDraftRecord record, CancellationToken cancellationToken)
    {
        var entity = new InvoiceDraftEntity
        {
            Id = record.Id,
            PayloadJson = record.PayloadJson,
            CanonicalSha256Hex = record.CanonicalSha256Hex,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc,
            IsImmutableSnapshot = record.IsImmutableSnapshot,
            ArchiveObjectKey = record.ArchiveObjectKey,
        };

        db.InvoiceDrafts.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task<InvoiceDraftRecord?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.InvoiceDrafts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        return new InvoiceDraftRecord(
            entity.Id,
            entity.PayloadJson,
            entity.CanonicalSha256Hex,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.IsImmutableSnapshot,
            entity.ArchiveObjectKey);
    }
}
