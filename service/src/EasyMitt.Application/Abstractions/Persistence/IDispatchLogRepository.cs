namespace EasyMitt.Application.Abstractions.Persistence;

public interface IDispatchLogRepository
{
    Task AddAsync(DispatchLogEntry entry, CancellationToken ct = default);

    Task<IReadOnlyList<DispatchLogDto>> GetByInvoiceAsync(Guid companyId, Guid invoiceId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, DispatchLogDto>> GetLatestByInvoicesAsync(Guid companyId, IReadOnlyList<Guid> invoiceIds, CancellationToken ct = default);
}

public sealed record DispatchLogEntry(
    Guid CompanyId,
    Guid InvoiceId,
    string Backend,
    string Status,
    string? PartnerId,
    string? ResponseJson);

public sealed record DispatchLogDto(
    Guid Id,
    Guid CompanyId,
    Guid InvoiceId,
    string Backend,
    string Status,
    string? PartnerId,
    string? ResponseJson,
    DateTime CreatedAtUtc);
