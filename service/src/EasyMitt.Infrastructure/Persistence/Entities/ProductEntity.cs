namespace EasyMitt.Infrastructure.Persistence.Entities;

public sealed class ProductEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public CompanyEntity? Company { get; set; }
    public string Type { get; set; } = "Product";
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Unit { get; set; } = "pcs";
    public decimal NetPrice { get; set; }
    public decimal VatRatePercent { get; set; } = 19m;
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
