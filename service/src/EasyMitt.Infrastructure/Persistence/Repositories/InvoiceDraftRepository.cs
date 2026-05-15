using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Domain.Billing;
using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EasyMitt.Infrastructure.Persistence.Repositories;

public sealed class InvoiceDraftRepository(EasyMittDbContext db) : IInvoiceDraftRepository
{
    public async Task<Guid> InsertAsync(InvoiceDraftRecord record, CancellationToken cancellationToken)
    {
        var entity = new InvoiceDraftEntity
        {
            Id = record.Id,
            CompanyId = record.CompanyId,
            CustomerId = record.CustomerId,
            LineProductIdsJson = JsonSerializer.Serialize(record.ProductIds),
            PayloadJson = record.PayloadJson,
            CanonicalSha256Hex = record.CanonicalSha256Hex,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc,
            Status = record.Status,
            IssuedAtUtc = record.IssuedAtUtc,
            SentAtUtc = record.SentAtUtc,
            PaidAtUtc = record.PaidAtUtc,
            CancelledAtUtc = record.CancelledAtUtc,
            IsImmutableSnapshot = record.IsImmutableSnapshot,
            ArchiveObjectKey = record.ArchiveObjectKey,
        };

        db.InvoiceDrafts.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task<IReadOnlyList<InvoiceDraftRecord>> ListAsync(Guid companyId, string? query, string? status, CancellationToken cancellationToken)
    {
        var normalizedQuery = query?.Trim().ToLowerInvariant();
        var normalizedStatus = status?.Trim();
        var records = db.InvoiceDrafts.AsNoTracking().Where(x => x.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(normalizedStatus) && InvoiceLifecycleStatus.IsKnown(normalizedStatus))
        {
            records = records.Where(x => x.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            records = records.Where(x =>
                x.PayloadJson.ToLower().Contains(normalizedQuery) ||
                x.CanonicalSha256Hex.ToLower().Contains(normalizedQuery));
        }

        var entities = await records
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        return entities.Select(ToRecord).ToList();
    }

    public async Task<InvoiceDraftRecord?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.InvoiceDrafts.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        return ToRecord(entity);
    }

    public async Task<InvoiceDraftRecord?> UpdateStatusAsync(Guid companyId, Guid id, string nextStatus, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var entity = await db.InvoiceDrafts.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (!InvoiceLifecyclePolicy.CanTransition(entity.Status, nextStatus))
        {
            throw new InvalidOperationException("invoice_lifecycle_transition_not_allowed");
        }

        entity.Status = nextStatus;
        entity.UpdatedAtUtc = nowUtc;

        switch (nextStatus)
        {
            case InvoiceLifecycleStatus.Issued:
                entity.IssuedAtUtc ??= nowUtc;
                break;
            case InvoiceLifecycleStatus.Sent:
                entity.SentAtUtc ??= nowUtc;
                break;
            case InvoiceLifecycleStatus.Paid:
                entity.PaidAtUtc ??= nowUtc;
                break;
            case InvoiceLifecycleStatus.Cancelled:
                entity.CancelledAtUtc ??= nowUtc;
                break;
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToRecord(entity);
    }

    private static InvoiceDraftRecord ToRecord(InvoiceDraftEntity entity) =>
        new(
            entity.Id,
            entity.CompanyId,
            entity.CustomerId,
            DeserializeProductIds(entity.LineProductIdsJson),
            entity.PayloadJson,
            entity.CanonicalSha256Hex,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.Status,
            entity.IssuedAtUtc,
            entity.SentAtUtc,
            entity.PaidAtUtc,
            entity.CancelledAtUtc,
            entity.IsImmutableSnapshot,
            entity.ArchiveObjectKey);

    private static IReadOnlyList<Guid?> DeserializeProductIds(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Guid?[]>(json) ?? Array.Empty<Guid?>();
        }
        catch (JsonException)
        {
            return Array.Empty<Guid?>();
        }
    }
}
