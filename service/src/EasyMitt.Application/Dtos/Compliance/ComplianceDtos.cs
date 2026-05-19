namespace EasyMitt.Application.Dtos.Compliance;

public sealed class ComplianceDashboardDto
{
    public ComplianceReadinessSummaryDto Readiness { get; init; } = new();
    public int TotalInvoices { get; init; }
    public int RiskyInvoices { get; init; }
    public int HighRiskInvoices { get; init; }
    public IReadOnlyList<ComplianceDocumentRiskDto> Documents { get; init; } = Array.Empty<ComplianceDocumentRiskDto>();
}

public sealed class ComplianceReadinessSummaryDto
{
    public int XRechnungReady { get; init; }
    public int XRechnungNotReady { get; init; }
    public int ZugferdReady { get; init; }
    public int ZugferdNotReady { get; init; }
    public int GobdArchived { get; init; }
    public int GobdNotArchived { get; init; }
    public int DatevExported { get; init; }
    public int DatevNotExported { get; init; }
    public int PaymentReconciled { get; init; }
    public int PaymentUnreconciled { get; init; }
    public int MahnwesenOverdueRisk { get; init; }
    public int SchematronReady { get; init; }
    public int SchematronNotReady { get; init; }
    public int Dispatched { get; init; }
    public int NotDispatched { get; init; }
}

public sealed class ComplianceDocumentRiskDto
{
    public Guid InvoiceDraftId { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public string Status { get; init; } = "";
    public DateOnly? IssueDate { get; init; }
    public decimal InvoiceTotal { get; init; }
    public string RiskLevel { get; init; } = "none";
    public IReadOnlyList<string> Risks { get; init; } = Array.Empty<string>();
    public bool IsGobdArchived { get; init; }
    public bool IsDatevExported { get; init; }
    public bool IsXRechnungReady { get; init; }
    public bool IsZugferdReady { get; init; }
    public bool IsSchematronValid { get; init; }
    public IReadOnlyList<string> SchematronFailureCodes { get; init; } = Array.Empty<string>();
    public bool IsDispatched { get; init; }
    public string? DispatchStatus { get; init; }
    public int DaysOverdue { get; init; }
    public int ReminderLevel { get; init; }
}

public sealed class ComplianceAuditEventDto
{
    public string EventType { get; init; } = "";
    public string Description { get; init; } = "";
    public DateTime OccurredAtUtc { get; init; }
    public string? ActorEmail { get; init; }
}

public sealed class ComplianceDocumentTimelineDto
{
    public Guid InvoiceDraftId { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public string Status { get; init; } = "";
    public IReadOnlyList<ComplianceAuditEventDto> Events { get; init; } = Array.Empty<ComplianceAuditEventDto>();
}
