using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Persistence.Repositories;

public sealed class DispatchLogRepository(EasyMittDbContext db) : IDispatchLogRepository
{
    public async Task AddAsync(DispatchLogEntry entry, CancellationToken ct = default)
    {
        db.DispatchLogs.Add(new DispatchLogEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = entry.CompanyId,
            InvoiceId = entry.InvoiceId,
            Backend = entry.Backend,
            Status = entry.Status,
            PartnerId = entry.PartnerId,
            ResponseJson = entry.ResponseJson,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DispatchLogDto>> GetByInvoiceAsync(Guid companyId, Guid invoiceId, CancellationToken ct = default)
    {
        return await db.DispatchLogs.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.InvoiceId == invoiceId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new DispatchLogDto(x.Id, x.CompanyId, x.InvoiceId, x.Backend, x.Status, x.PartnerId, x.ResponseJson, x.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, DispatchLogDto>> GetLatestByInvoicesAsync(Guid companyId, IReadOnlyList<Guid> invoiceIds, CancellationToken ct = default)
    {
        if (invoiceIds.Count == 0)
            return new Dictionary<Guid, DispatchLogDto>();

        var rows = await db.DispatchLogs.AsNoTracking()
            .Where(x => x.CompanyId == companyId && invoiceIds.Contains(x.InvoiceId))
            .GroupBy(x => x.InvoiceId)
            .Select(g => g.OrderByDescending(x => x.CreatedAtUtc).First())
            .ToListAsync(ct);

        return rows.ToDictionary(
            x => x.InvoiceId,
            x => new DispatchLogDto(x.Id, x.CompanyId, x.InvoiceId, x.Backend, x.Status, x.PartnerId, x.ResponseJson, x.CreatedAtUtc));
    }
}
