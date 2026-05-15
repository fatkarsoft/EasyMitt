namespace EasyMitt.Application.Dtos.Dunning;

public sealed class DunningInvoiceDto
{
    public Guid InvoiceDraftId { get; init; }
    public Guid? CustomerId { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public string? CustomerEmail { get; init; }
    public DateOnly IssueDate { get; init; }
    public DateOnly DueDate { get; init; }
    public int DaysOverdue { get; init; }
    public decimal InvoiceTotal { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal OpenAmount { get; init; }
    public string InvoiceStatus { get; init; } = "";
    public int ReminderLevel { get; init; }
    public DateTime? LastReminderAtUtc { get; init; }
}

public sealed class DunningReminderDto
{
    public Guid Id { get; init; }
    public Guid InvoiceDraftId { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public int Level { get; init; }
    public string LevelName { get; init; } = "";
    public decimal OpenAmount { get; init; }
    public string? Note { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public Guid UserId { get; init; }
    public string UserEmail { get; init; } = "";
}

public sealed class DunningReminderCreateDto
{
    public Guid InvoiceDraftId { get; init; }
    public string? Note { get; init; }
}

public sealed class DunningCustomerSummaryDto
{
    public Guid? CustomerId { get; init; }
    public string CustomerName { get; init; } = "";
    public int OverdueInvoiceCount { get; init; }
    public decimal OpenAmount { get; init; }
    public int HighestReminderLevel { get; init; }
    public DateTime? LastReminderAtUtc { get; init; }
}

public sealed class DunningOverviewDto
{
    public IReadOnlyList<DunningInvoiceDto> Invoices { get; init; } = Array.Empty<DunningInvoiceDto>();
    public IReadOnlyList<DunningCustomerSummaryDto> Customers { get; init; } = Array.Empty<DunningCustomerSummaryDto>();
    public decimal TotalOpenAmount { get; init; }
    public int OverdueInvoiceCount { get; init; }
    public int ReminderDueCount { get; init; }
    public decimal CollectedAmount { get; init; }
}
