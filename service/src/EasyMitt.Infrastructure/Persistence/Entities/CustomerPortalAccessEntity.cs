namespace EasyMitt.Infrastructure.Persistence.Entities;

public sealed class CustomerPortalAccessEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public CompanyEntity? Company { get; set; }
    public Guid CustomerId { get; set; }
    public CustomerEntity? Customer { get; set; }
    public string Label { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public string TokenPrefix { get; set; } = "";
    public string Status { get; set; } = "Active";
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedByUserEmail { get; set; } = "";
    public DateTime? LastUsedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}
