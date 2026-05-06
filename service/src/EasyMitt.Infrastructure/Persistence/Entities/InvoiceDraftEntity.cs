namespace EasyMitt.Infrastructure.Persistence.Entities;

public sealed class InvoiceDraftEntity
{
    public Guid Id { get; set; }

    public string PayloadJson { get; set; } = "";

    public string CanonicalSha256Hex { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public bool IsImmutableSnapshot { get; set; }

    public string? ArchiveObjectKey { get; set; }
}
