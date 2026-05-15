namespace EasyMitt.Infrastructure.Persistence.Entities;

public sealed class InvoiceDraftEntity
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public CompanyEntity? Company { get; set; }

    public Guid? CustomerId { get; set; }

    public CustomerEntity? Customer { get; set; }

    public string LineProductIdsJson { get; set; } = "[]";

    public string PayloadJson { get; set; } = "";

    public string CanonicalSha256Hex { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string Status { get; set; } = "Draft";

    public DateTime? IssuedAtUtc { get; set; }

    public DateTime? SentAtUtc { get; set; }

    public DateTime? PaidAtUtc { get; set; }

    public DateTime? CancelledAtUtc { get; set; }

    public bool IsImmutableSnapshot { get; set; }

    public string? ArchiveObjectKey { get; set; }
}
