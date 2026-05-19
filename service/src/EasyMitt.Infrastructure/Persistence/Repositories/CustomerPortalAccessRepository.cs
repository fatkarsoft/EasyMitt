using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Portal;
using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Persistence.Repositories;

public sealed class CustomerPortalAccessRepository(EasyMittDbContext db) : ICustomerPortalAccessRepository
{
    public async Task<PortalAccessRecord> CreateAsync(
        Guid companyId,
        Guid customerId,
        string label,
        string tokenHash,
        string tokenPrefix,
        DateTime? expiresAtUtc,
        string createdByUserEmail,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var entity = new CustomerPortalAccessEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CustomerId = customerId,
            Label = string.IsNullOrWhiteSpace(label) ? "Portal" : label.Trim(),
            TokenHash = tokenHash,
            TokenPrefix = tokenPrefix,
            Status = "Active",
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = now,
            CreatedByUserEmail = createdByUserEmail
        };
        db.CustomerPortalAccesses.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToRecord(entity);
    }

    public async Task<IReadOnlyList<PortalAccessRecord>> ListForCustomerAsync(
        Guid companyId,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var rows = await db.CustomerPortalAccesses
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return rows.Select(ToRecord).ToArray();
    }

    public async Task<bool> RevokeAsync(Guid companyId, Guid tokenId, CancellationToken cancellationToken)
    {
        var entity = await db.CustomerPortalAccesses
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == tokenId, cancellationToken);
        if (entity is null) return false;
        if (entity.Status == "Revoked") return true;
        entity.Status = "Revoked";
        entity.RevokedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PortalAccessRecord?> FindActiveByTokenHashAsync(string tokenHash, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var entity = await db.CustomerPortalAccesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (entity is null) return null;
        if (entity.Status != "Active") return null;
        if (entity.ExpiresAtUtc.HasValue && entity.ExpiresAtUtc.Value <= nowUtc) return null;
        return ToRecord(entity);
    }

    public async Task TouchUsageAsync(Guid tokenId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var entity = await db.CustomerPortalAccesses.FirstOrDefaultAsync(x => x.Id == tokenId, cancellationToken);
        if (entity is null) return;
        entity.LastUsedAtUtc = nowUtc;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static PortalAccessRecord ToRecord(CustomerPortalAccessEntity entity) => new(
        entity.Id,
        entity.CompanyId,
        entity.CustomerId,
        entity.Label,
        entity.TokenPrefix,
        entity.Status,
        entity.ExpiresAtUtc,
        entity.CreatedAtUtc,
        entity.CreatedByUserEmail,
        entity.LastUsedAtUtc,
        entity.RevokedAtUtc);
}
