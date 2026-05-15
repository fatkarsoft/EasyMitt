namespace EasyMitt.Infrastructure.Persistence.Entities;

public sealed class CustomerEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public CompanyEntity? Company { get; set; }
    public string Type { get; set; } = "Business";
    public string DisplayName { get; set; } = "";
    public string? CompanyName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Street { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string CountryCode { get; set; } = "DE";
    public string? VatId { get; set; }
    public string? TaxNumber { get; set; }
    public string? LeitwegId { get; set; }
    public string? DatevDebitorAccount { get; set; }
    public int PaymentTermsDays { get; set; } = 14;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
