namespace EasyMitt.Infrastructure.Persistence.Entities;

public sealed class EmailDeliveryLogEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string DocumentType { get; set; } = "";
    public Guid DocumentId { get; set; }
    public string ToEmail { get; set; } = "";
    public string Subject { get; set; } = "";
    public string AttachmentType { get; set; } = "";
    public string Status { get; set; } = "";
    public string? ErrorMessage { get; set; }
    public string SenderUserId { get; set; } = "";
    public string SenderUserEmail { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }

    public CompanyEntity? Company { get; set; }
}
