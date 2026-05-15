namespace EasyMitt.Application.Dtos.Customers;

public sealed class CustomerDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "Business";
    public string DisplayName { get; init; } = "";
    public string? CompanyName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Street { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public string CountryCode { get; init; } = "DE";
    public string? VatId { get; init; }
    public string? TaxNumber { get; init; }
    public string? LeitwegId { get; init; }
    public string? DatevDebitorAccount { get; init; }
    public int PaymentTermsDays { get; init; } = 14;
    public string? Notes { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class CustomerUpsertDto
{
    public string Type { get; init; } = "Business";
    public string? CompanyName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Street { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public string CountryCode { get; init; } = "DE";
    public string? VatId { get; init; }
    public string? TaxNumber { get; init; }
    public string? LeitwegId { get; init; }
    public string? DatevDebitorAccount { get; init; }
    public int PaymentTermsDays { get; init; } = 14;
    public string? Notes { get; init; }
    public bool IsActive { get; init; } = true;
}
