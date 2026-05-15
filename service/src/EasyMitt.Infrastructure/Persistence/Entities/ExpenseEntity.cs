namespace EasyMitt.Infrastructure.Persistence.Entities;

public sealed class ExpenseEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public CompanyEntity? Company { get; set; }
    public string VendorName { get; set; } = "";
    public string? DocumentNumber { get; set; }
    public DateOnly IssueDate { get; set; }
    public string Category { get; set; } = "General";
    public decimal NetAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "EUR";
    public string? DatevCreditorAccount { get; set; }
    public string Status { get; set; } = "Inbox";
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
