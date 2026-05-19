using EasyMitt.Application.Dtos.Portal;

namespace EasyMitt.Application.Abstractions.Persistence;

public interface ICustomerPortalAccessRepository
{
    Task<PortalAccessRecord> CreateAsync(
        Guid companyId,
        Guid customerId,
        string label,
        string tokenHash,
        string tokenPrefix,
        DateTime? expiresAtUtc,
        string createdByUserEmail,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PortalAccessRecord>> ListForCustomerAsync(
        Guid companyId,
        Guid customerId,
        CancellationToken cancellationToken);

    Task<bool> RevokeAsync(Guid companyId, Guid tokenId, CancellationToken cancellationToken);

    Task<PortalAccessRecord?> FindActiveByTokenHashAsync(string tokenHash, DateTime nowUtc, CancellationToken cancellationToken);

    Task TouchUsageAsync(Guid tokenId, DateTime nowUtc, CancellationToken cancellationToken);
}

public sealed record PortalAccessRecord(
    Guid Id,
    Guid CompanyId,
    Guid CustomerId,
    string Label,
    string TokenPrefix,
    string Status,
    DateTime? ExpiresAtUtc,
    DateTime CreatedAtUtc,
    string CreatedByUserEmail,
    DateTime? LastUsedAtUtc,
    DateTime? RevokedAtUtc);
