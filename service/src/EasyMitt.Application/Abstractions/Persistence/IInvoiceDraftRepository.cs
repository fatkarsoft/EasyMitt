using EasyMitt.Application.Dtos.En16931;

namespace EasyMitt.Application.Abstractions.Persistence;

public interface IInvoiceDraftRepository
{
    Task<Guid> InsertAsync(InvoiceDraftRecord record, CancellationToken cancellationToken);

    Task<IReadOnlyList<InvoiceDraftRecord>> ListAsync(Guid companyId, string? query, string? status, CancellationToken cancellationToken);

    Task<InvoiceDraftRecord?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken);

    Task<InvoiceDraftRecord?> UpdateStatusAsync(Guid companyId, Guid id, string nextStatus, DateTime nowUtc, CancellationToken cancellationToken);
}

public sealed record InvoiceDraftRecord(
    Guid Id,
    Guid CompanyId,
    Guid? CustomerId,
    IReadOnlyList<Guid?> ProductIds,
    string PayloadJson,
    string CanonicalSha256Hex,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string Status,
    DateTime? IssuedAtUtc,
    DateTime? SentAtUtc,
    DateTime? PaidAtUtc,
    DateTime? CancelledAtUtc,
    bool IsImmutableSnapshot,
    string? ArchiveObjectKey);
