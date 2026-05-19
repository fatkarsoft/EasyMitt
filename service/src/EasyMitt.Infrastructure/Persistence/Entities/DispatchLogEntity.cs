namespace EasyMitt.Infrastructure.Persistence.Entities;

public sealed class DispatchLogEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid InvoiceId { get; set; }
    public string Backend { get; set; } = "";
    public string Status { get; set; } = "";
    public string? PartnerId { get; set; }
    public string? ResponseJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public CompanyEntity? Company { get; set; }
}
