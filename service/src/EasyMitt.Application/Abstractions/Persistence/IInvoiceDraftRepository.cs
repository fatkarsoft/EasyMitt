using EasyMitt.Application.Dtos.En16931;

namespace EasyMitt.Application.Abstractions.Persistence;

public interface IInvoiceDraftRepository
{
    Task<Guid> InsertAsync(InvoiceDraftRecord record, CancellationToken cancellationToken);

    Task<InvoiceDraftRecord?> GetAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record InvoiceDraftRecord(
    Guid Id,
    string PayloadJson,
    string CanonicalSha256Hex,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    bool IsImmutableSnapshot,
    string? ArchiveObjectKey);
