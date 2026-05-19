namespace EasyMitt.Application.Abstractions.Archiving;

public interface IArchiveVerifier
{
    Task<ArchiveVerificationResult> VerifyInvoiceAsync(Guid companyId, Guid invoiceId, CancellationToken ct = default);
}

public sealed record ArchiveVerificationResult(
    bool Found,
    bool HashMatches,
    string? ExpectedSha256Hex,
    string? ActualSha256Hex,
    string? ArchiveObjectKey,
    string? Backend);
