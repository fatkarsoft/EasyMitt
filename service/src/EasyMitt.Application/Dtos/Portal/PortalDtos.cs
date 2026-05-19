namespace EasyMitt.Application.Dtos.Portal;

public sealed class PortalAccessTokenDto
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public string Label { get; init; } = "";
    public string TokenPrefix { get; init; } = "";
    public string Status { get; init; } = "Active";
    public DateTime? ExpiresAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public string CreatedByUserEmail { get; init; } = "";
    public DateTime? LastUsedAtUtc { get; init; }
    public DateTime? RevokedAtUtc { get; init; }
}

public sealed class PortalAccessTokenIssuedDto
{
    public PortalAccessTokenDto Access { get; init; } = new();
    public string Token { get; init; } = "";
    public string PortalUrl { get; init; } = "";
}

public sealed class PortalAccessIssueRequestDto
{
    public string? Label { get; init; }
    public int? ValidityDays { get; init; }
}

public sealed class PortalSessionDto
{
    public Guid CustomerId { get; init; }
    public string CustomerDisplayName { get; init; } = "";
    public string CompanyName { get; init; } = "";
    public string TokenLabel { get; init; } = "";
    public DateTime? ExpiresAtUtc { get; init; }
    public DateTime? LastUsedAtUtc { get; init; }
}

public sealed class PortalInvoiceListItemDto
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public DateOnly? IssueDate { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal AmountPaid { get; init; }
    public decimal AmountOpen { get; init; }
    public string Status { get; init; } = "Draft";
    public bool IsOverdue { get; init; }
    public DateTime? IssuedAtUtc { get; init; }
    public DateTime? PaidAtUtc { get; init; }
}

public sealed class PortalInvoiceDetailDto
{
    public PortalInvoiceListItemDto Summary { get; init; } = new();
    public string PayloadJson { get; init; } = "";
    public IReadOnlyList<PortalPaymentDto> Payments { get; init; } = Array.Empty<PortalPaymentDto>();
}

public sealed class PortalPaymentDto
{
    public DateOnly BookingDate { get; init; }
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = "EUR";
    public string Description { get; init; } = "";
}

public sealed class PortalQuoteListItemDto
{
    public Guid Id { get; init; }
    public string QuoteNumber { get; init; } = "";
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = "Draft";
    public DateTime ValidUntilUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? SentAtUtc { get; init; }
    public DateTime? AcceptedAtUtc { get; init; }
    public DateTime? DeclinedAtUtc { get; init; }
}

public sealed class PortalQuoteDetailDto
{
    public PortalQuoteListItemDto Summary { get; init; } = new();
    public string PayloadJson { get; init; } = "";
}
