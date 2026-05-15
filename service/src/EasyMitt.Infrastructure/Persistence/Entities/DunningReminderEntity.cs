namespace EasyMitt.Infrastructure.Persistence.Entities;

public sealed class DunningReminderEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid InvoiceDraftId { get; set; }
    public int Level { get; set; }
    public decimal OpenAmount { get; set; }
    public string? Note { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }

    public CompanyEntity? Company { get; set; }
    public InvoiceDraftEntity? InvoiceDraft { get; set; }
}
