using System.Security.Cryptography;
using EasyMitt.Application.Abstractions.Archiving;
using EasyMitt.Application.Abstractions.Persistence;
using Microsoft.Extensions.Options;

namespace EasyMitt.Infrastructure.Archiving;

public sealed class ArchiveVerifier(
    IInvoiceDraftRepository draftRepository,
    IImmutableArchiveStore archiveStore,
    IOptions<ArchiveOptions> archiveOptionsAccessor) : IArchiveVerifier
{
    public async Task<ArchiveVerificationResult> VerifyInvoiceAsync(Guid companyId, Guid invoiceId, CancellationToken ct = default)
    {
        var record = await draftRepository.GetAsync(companyId, invoiceId, ct);
        if (record is null)
            return new ArchiveVerificationResult(false, false, null, null, null, archiveOptionsAccessor.Value.Backend);

        if (string.IsNullOrWhiteSpace(record.ArchiveObjectKey))
            return new ArchiveVerificationResult(false, false, record.CanonicalSha256Hex, null, null, archiveOptionsAccessor.Value.Backend);

        var bytes = await archiveStore.ReadAsync(record.ArchiveObjectKey, ct);
        if (bytes is null)
            return new ArchiveVerificationResult(false, false, record.CanonicalSha256Hex, null, record.ArchiveObjectKey, archiveOptionsAccessor.Value.Backend);

        var actualHex = Convert.ToHexString(SHA256.HashData(bytes));
        var matches = string.Equals(actualHex, record.CanonicalSha256Hex, StringComparison.OrdinalIgnoreCase);
        return new ArchiveVerificationResult(true, matches, record.CanonicalSha256Hex, actualHex, record.ArchiveObjectKey, archiveOptionsAccessor.Value.Backend);
    }
}
