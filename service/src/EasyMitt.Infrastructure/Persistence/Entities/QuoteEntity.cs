namespace EasyMitt.Infrastructure.Persistence.Entities;

public sealed class QuoteEntity
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public CompanyEntity? Company { get; set; }

    public Guid? CustomerId { get; set; }

    public CustomerEntity? Customer { get; set; }

    public string LineProductIdsJson { get; set; } = "[]";

    public string PayloadJson { get; set; } = "";

    public string QuoteNumber { get; set; } = "";

    public string Status { get; set; } = "Draft";

    public decimal TotalAmount { get; set; }

    public DateTime ValidUntilUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? SentAtUtc { get; set; }

    public DateTime? AcceptedAtUtc { get; set; }

    public DateTime? DeclinedAtUtc { get; set; }

    public DateTime? ConvertedAtUtc { get; set; }

    public Guid? ConvertedInvoiceDraftId { get; set; }
}
