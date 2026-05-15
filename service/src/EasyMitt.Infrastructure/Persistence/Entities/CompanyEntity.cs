namespace EasyMitt.Infrastructure.Persistence.Entities;

public sealed class CompanyEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string CountryCode { get; set; } = "DE";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
