namespace EasyMitt.Application.Dtos.Expenses;

public sealed class ExpenseDto
{
    public Guid Id { get; init; }
    public string VendorName { get; init; } = "";
    public string? DocumentNumber { get; init; }
    public DateOnly IssueDate { get; init; }
    public string Category { get; init; } = "General";
    public decimal NetAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string CurrencyCode { get; init; } = "EUR";
    public string? DatevCreditorAccount { get; init; }
    public string Status { get; init; } = "Inbox";
    public string? Notes { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class ExpenseUpsertDto
{
    public string VendorName { get; init; } = "";
    public string? DocumentNumber { get; init; }
    public DateOnly IssueDate { get; init; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string Category { get; init; } = "General";
    public decimal NetAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string CurrencyCode { get; init; } = "EUR";
    public string? DatevCreditorAccount { get; init; }
    public string? Notes { get; init; }
}
