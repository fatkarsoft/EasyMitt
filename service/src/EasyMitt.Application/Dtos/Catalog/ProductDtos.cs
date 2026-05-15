namespace EasyMitt.Application.Dtos.Catalog;

public sealed class ProductDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "Product";
    public string Sku { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public string Unit { get; init; } = "pcs";
    public decimal NetPrice { get; init; }
    public decimal VatRatePercent { get; init; } = 19m;
    public decimal CurrentStock { get; init; }
    public decimal MinimumStock { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class ProductUpsertDto
{
    public string Type { get; init; } = "Product";
    public string Sku { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public string Unit { get; init; } = "pcs";
    public decimal NetPrice { get; init; }
    public decimal VatRatePercent { get; init; } = 19m;
    public decimal CurrentStock { get; init; }
    public decimal MinimumStock { get; init; }
    public bool IsActive { get; init; } = true;
}
