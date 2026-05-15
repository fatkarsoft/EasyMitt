namespace EasyMitt.Infrastructure.Persistence.Entities;

public sealed class BankTransactionEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public DateOnly BookingDate { get; set; }
    public string Description { get; set; } = "";
    public string? CounterpartyName { get; set; }
    public string? CounterpartyIban { get; set; }
    public decimal Amount { get; set; }
    public string Direction { get; set; } = "Incoming";
    public string CurrencyCode { get; set; } = "EUR";
    public string Status { get; set; } = "Unmatched";
    public string? Source { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public CompanyEntity? Company { get; set; }
    public List<PaymentAllocationEntity> Allocations { get; set; } = new();
}
