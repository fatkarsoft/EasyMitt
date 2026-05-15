namespace EasyMitt.Infrastructure.Persistence.Entities;

public sealed class InventoryMovementEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public CompanyEntity? Company { get; set; }
    public Guid ProductId { get; set; }
    public ProductEntity? Product { get; set; }
    public string Type { get; set; } = "Adjustment";
    public decimal QuantityDelta { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
