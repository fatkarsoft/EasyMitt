using EasyMitt.Application.Dtos.En16931;

namespace EasyMitt.Application.Dtos.Quotes;

public sealed class QuoteDto
{
    public Guid Id { get; init; }
    public Guid? CustomerId { get; init; }
    public IReadOnlyList<Guid?> ProductIds { get; init; } = Array.Empty<Guid?>();
    public string QuoteNumber { get; init; } = "";
    public string Status { get; init; } = "Draft";
    public decimal TotalAmount { get; init; }
    public DateTime ValidUntilUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public DateTime? SentAtUtc { get; init; }
    public DateTime? AcceptedAtUtc { get; init; }
    public DateTime? DeclinedAtUtc { get; init; }
    public DateTime? ConvertedAtUtc { get; init; }
    public Guid? ConvertedInvoiceDraftId { get; init; }
    public InvoiceDocumentDto Document { get; init; } = new();
}

public sealed class QuoteUpsertDto
{
    public Guid? CustomerId { get; init; }
    public IReadOnlyList<Guid?> ProductIds { get; init; } = Array.Empty<Guid?>();
    public DateTime? ValidUntilUtc { get; init; }
    public InvoiceDocumentDto Document { get; init; } = new();
}
