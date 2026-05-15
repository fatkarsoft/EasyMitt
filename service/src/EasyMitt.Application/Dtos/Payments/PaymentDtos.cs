namespace EasyMitt.Application.Dtos.Payments;

public sealed class BankTransactionDto
{
    public Guid Id { get; init; }
    public DateOnly BookingDate { get; init; }
    public string Description { get; init; } = "";
    public string? CounterpartyName { get; init; }
    public string? CounterpartyIban { get; init; }
    public decimal Amount { get; init; }
    public decimal MatchedAmount { get; init; }
    public decimal UnmatchedAmount { get; init; }
    public string Direction { get; init; } = "Incoming";
    public string CurrencyCode { get; init; } = "EUR";
    public string Status { get; init; } = "Unmatched";
    public string? Source { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public IReadOnlyList<PaymentAllocationDto> Allocations { get; init; } = Array.Empty<PaymentAllocationDto>();
}

public sealed class BankTransactionCreateDto
{
    public DateOnly BookingDate { get; init; }
    public string Description { get; init; } = "";
    public string? CounterpartyName { get; init; }
    public string? CounterpartyIban { get; init; }
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = "EUR";
    public string? Source { get; init; }
}

public sealed class PaymentAllocationDto
{
    public Guid Id { get; init; }
    public Guid BankTransactionId { get; init; }
    public Guid InvoiceDraftId { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public decimal Amount { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public sealed class PaymentAllocationCreateDto
{
    public Guid BankTransactionId { get; init; }
    public Guid InvoiceDraftId { get; init; }
    public decimal Amount { get; init; }
}

public sealed class PaymentSuggestionDto
{
    public Guid InvoiceDraftId { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public string BuyerName { get; init; } = "";
    public DateOnly IssueDate { get; init; }
    public decimal InvoiceTotal { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal OpenAmount { get; init; }
    public int Score { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = Array.Empty<string>();
}

public sealed class InvoicePaymentSummaryDto
{
    public Guid InvoiceDraftId { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public decimal InvoiceTotal { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal OpenAmount { get; init; }
    public string PaymentStatus { get; init; } = "Unpaid";
    public IReadOnlyList<PaymentAllocationDto> Allocations { get; init; } = Array.Empty<PaymentAllocationDto>();
}
