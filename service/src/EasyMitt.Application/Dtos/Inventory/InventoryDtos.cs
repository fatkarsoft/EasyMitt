namespace EasyMitt.Application.Dtos.Inventory;

public sealed class InventoryMovementDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = "";
    public string ProductSku { get; init; } = "";
    public string Type { get; init; } = "Adjustment";
    public decimal QuantityDelta { get; init; }
    public string? Reason { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public sealed class InventoryMovementCreateDto
{
    public Guid ProductId { get; init; }
    public string Type { get; init; } = "Adjustment";
    public decimal QuantityDelta { get; init; }
    public string? Reason { get; init; }
}
