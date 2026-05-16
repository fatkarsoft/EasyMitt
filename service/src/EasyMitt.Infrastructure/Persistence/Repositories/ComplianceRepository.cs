using System.Globalization;
using System.Text.Json;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Compliance;
using EasyMitt.Domain.Billing;
using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Persistence.Repositories;

public sealed class ComplianceRepository(EasyMittDbContext db) : IComplianceRepository
{
    public async Task<ComplianceDashboardDto> GetDashboardAsync(
        Guid companyId,
        DateOnly today,
        DateOnly? from,
        DateOnly? to,
        string? status,
        string? riskLevel,
        CancellationToken cancellationToken)
    {
        var invoiceQuery = db.InvoiceDrafts.AsNoTracking()
            .Include(x => x.Customer)
            .Where(x => x.CompanyId == companyId);

        var invoices = await invoiceQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(500)
            .ToListAsync(cancellationToken);

        var invoiceIds = invoices.Select(x => x.Id).ToArray();

        var paidByInvoice = await db.PaymentAllocations.AsNoTracking()
            .Where(x => x.CompanyId == companyId && invoiceIds.Contains(x.InvoiceDraftId))
            .GroupBy(x => x.InvoiceDraftId)
            .Select(x => new { InvoiceDraftId = x.Key, Amount = x.Sum(y => y.Amount) })
            .ToDictionaryAsync(x => x.InvoiceDraftId, x => x.Amount, cancellationToken);

        var remindersByInvoice = await db.DunningReminders.AsNoTracking()
            .Where(x => x.CompanyId == companyId && invoiceIds.Contains(x.InvoiceDraftId))
            .GroupBy(x => x.InvoiceDraftId)
            .Select(x => new ReminderSummary(x.Key, x.Max(y => y.Level), x.Max(y => y.CreatedAtUtc)))
            .ToDictionaryAsync(x => x.InvoiceDraftId, x => x, cancellationToken);

        var datevLogs = await db.DatevExportLogs.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.PeriodFrom != null && x.PeriodTo != null)
            .Select(x => new { x.PeriodFrom, x.PeriodTo })
            .ToListAsync(cancellationToken);

        var documents = new List<ComplianceDocumentRiskDto>();

        foreach (var invoice in invoices)
        {
            var issueDate = ParseIssueDate(invoice.PayloadJson, DateOnly.FromDateTime(invoice.CreatedAtUtc));

            if (from.HasValue && issueDate < from.Value) continue;
            if (to.HasValue && issueDate > to.Value) continue;
            if (!string.IsNullOrEmpty(status) && !string.Equals(invoice.Status, status, StringComparison.OrdinalIgnoreCase)) continue;

            var doc = BuildDocumentRisk(invoice, today, issueDate, paidByInvoice, remindersByInvoice, datevLogs.Select(x => (x.PeriodFrom!.Value, x.PeriodTo!.Value)).ToArray());

            if (!string.IsNullOrEmpty(riskLevel) && !string.Equals(doc.RiskLevel, riskLevel, StringComparison.OrdinalIgnoreCase)) continue;

            documents.Add(doc);
        }

        var nonDraftDocs = documents.Where(x => x.Status != InvoiceLifecycleStatus.Draft).ToArray();

        var readiness = new ComplianceReadinessSummaryDto
        {
            XRechnungReady = nonDraftDocs.Count(x => x.IsXRechnungReady),
            XRechnungNotReady = nonDraftDocs.Count(x => !x.IsXRechnungReady),
            ZugferdReady = nonDraftDocs.Count(x => x.IsZugferdReady),
            ZugferdNotReady = nonDraftDocs.Count(x => !x.IsZugferdReady),
            GobdArchived = nonDraftDocs.Count(x => x.IsGobdArchived),
            GobdNotArchived = nonDraftDocs.Count(x => !x.IsGobdArchived),
            DatevExported = nonDraftDocs.Count(x => x.IsDatevExported),
            DatevNotExported = nonDraftDocs.Count(x => !x.IsDatevExported),
            PaymentReconciled = nonDraftDocs.Count(x => x.Status is InvoiceLifecycleStatus.Paid or InvoiceLifecycleStatus.PartiallyPaid),
            PaymentUnreconciled = nonDraftDocs.Count(x => x.Status is not InvoiceLifecycleStatus.Paid and not InvoiceLifecycleStatus.PartiallyPaid and not InvoiceLifecycleStatus.Cancelled),
            MahnwesenOverdueRisk = nonDraftDocs.Count(x => x.DaysOverdue > 0 && x.ReminderLevel == 0),
        };

        var sorted = documents
            .OrderByDescending(x => RiskOrder(x.RiskLevel))
            .ThenByDescending(x => x.DaysOverdue)
            .ToArray();

        return new ComplianceDashboardDto
        {
            Readiness = readiness,
            TotalInvoices = documents.Count,
            RiskyInvoices = documents.Count(x => x.RiskLevel is not "none"),
            HighRiskInvoices = documents.Count(x => x.RiskLevel is "high"),
            Documents = sorted,
        };
    }

    public async Task<ComplianceDocumentTimelineDto?> GetDocumentTimelineAsync(
        Guid companyId,
        Guid invoiceDraftId,
        CancellationToken cancellationToken)
    {
        var invoice = await db.InvoiceDrafts.AsNoTracking()
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == invoiceDraftId, cancellationToken);

        if (invoice is null) return null;

        var reminders = await db.DunningReminders.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.InvoiceDraftId == invoiceDraftId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var events = new List<ComplianceAuditEventDto>();

        events.Add(new ComplianceAuditEventDto
        {
            EventType = "created",
            Description = "invoice_created",
            OccurredAtUtc = invoice.CreatedAtUtc,
        });

        if (invoice.IssuedAtUtc.HasValue)
        {
            events.Add(new ComplianceAuditEventDto
            {
                EventType = "issued",
                Description = "invoice_issued",
                OccurredAtUtc = invoice.IssuedAtUtc.Value,
            });
        }

        if (invoice.SentAtUtc.HasValue)
        {
            events.Add(new ComplianceAuditEventDto
            {
                EventType = "sent",
                Description = "invoice_sent",
                OccurredAtUtc = invoice.SentAtUtc.Value,
            });
        }

        if (!string.IsNullOrEmpty(invoice.ArchiveObjectKey))
        {
            events.Add(new ComplianceAuditEventDto
            {
                EventType = "archived",
                Description = "invoice_archived",
                OccurredAtUtc = invoice.IssuedAtUtc ?? invoice.CreatedAtUtc,
            });
        }

        foreach (var reminder in reminders)
        {
            events.Add(new ComplianceAuditEventDto
            {
                EventType = "dunning",
                Description = $"dunning_level_{reminder.Level}",
                OccurredAtUtc = reminder.CreatedAtUtc,
                ActorEmail = reminder.UserEmail,
            });
        }

        if (invoice.PaidAtUtc.HasValue)
        {
            events.Add(new ComplianceAuditEventDto
            {
                EventType = "paid",
                Description = "invoice_paid",
                OccurredAtUtc = invoice.PaidAtUtc.Value,
            });
        }

        if (invoice.CancelledAtUtc.HasValue)
        {
            events.Add(new ComplianceAuditEventDto
            {
                EventType = "cancelled",
                Description = "invoice_cancelled",
                OccurredAtUtc = invoice.CancelledAtUtc.Value,
            });
        }

        return new ComplianceDocumentTimelineDto
        {
            InvoiceDraftId = invoice.Id,
            InvoiceNumber = ReadString(invoice.PayloadJson, "core", "BT-1"),
            Status = invoice.Status,
            Events = events.OrderBy(x => x.OccurredAtUtc).ToArray(),
        };
    }

    private static ComplianceDocumentRiskDto BuildDocumentRisk(
        InvoiceDraftEntity invoice,
        DateOnly today,
        DateOnly issueDate,
        Dictionary<Guid, decimal> paidByInvoice,
        Dictionary<Guid, ReminderSummary> remindersByInvoice,
        (DateOnly From, DateOnly To)[] datevPeriods)
    {
        var invoiceNumber = ReadString(invoice.PayloadJson, "core", "BT-1");
        var currencyCode = ReadString(invoice.PayloadJson, "core", "BT-5");
        var sellerName = ReadString(invoice.PayloadJson, "seller", "BT-20");
        var sellerVatId = ReadString(invoice.PayloadJson, "seller", "BT-22");
        var sellerIban = ReadString(invoice.PayloadJson, "seller", "BT-34");
        var buyerName = ReadString(invoice.PayloadJson, "buyer", "BT-26");
        var total = ReadDecimal(invoice.PayloadJson, "core", "BT-112");
        var customerName = invoice.Customer?.DisplayName ?? buyerName;

        var isXRechnungReady = !string.IsNullOrWhiteSpace(invoiceNumber)
            && issueDate != default
            && !string.IsNullOrWhiteSpace(currencyCode)
            && !string.IsNullOrWhiteSpace(sellerName)
            && !string.IsNullOrWhiteSpace(sellerVatId)
            && !string.IsNullOrWhiteSpace(sellerIban)
            && !string.IsNullOrWhiteSpace(buyerName);

        var isGobdArchived = !string.IsNullOrEmpty(invoice.ArchiveObjectKey);
        var isDatevExported = datevPeriods.Any(p => issueDate >= p.From && issueDate <= p.To);
        var daysOverdue = Math.Max(0, today.DayNumber - issueDate.AddDays(invoice.Customer?.PaymentTermsDays ?? 14).DayNumber);
        var reminderLevel = remindersByInvoice.TryGetValue(invoice.Id, out var reminderState) ? reminderState.MaxLevel : 0;
        var paid = paidByInvoice.GetValueOrDefault(invoice.Id);
        var openAmount = Math.Max(0, total - paid);

        var risks = new List<string>();

        if (string.IsNullOrWhiteSpace(invoiceNumber)) risks.Add("missing_invoice_number");
        if (issueDate == default) risks.Add("missing_issue_date");
        if (string.IsNullOrWhiteSpace(sellerName)) risks.Add("missing_seller_name");
        if (string.IsNullOrWhiteSpace(sellerVatId)) risks.Add("missing_seller_vat");
        if (string.IsNullOrWhiteSpace(sellerIban)) risks.Add("missing_seller_iban");
        if (string.IsNullOrWhiteSpace(buyerName)) risks.Add("missing_buyer_name");

        if (invoice.Status != InvoiceLifecycleStatus.Draft && invoice.Status != InvoiceLifecycleStatus.Cancelled)
        {
            if (!isGobdArchived) risks.Add("not_gobd_archived");
            if (!isDatevExported) risks.Add("not_datev_exported");
        }

        if (daysOverdue > 0 && openAmount > 0 && reminderLevel == 0)
            risks.Add("overdue_no_reminder");

        var riskLevel = DetermineRiskLevel(risks, invoice.Status);

        return new ComplianceDocumentRiskDto
        {
            InvoiceDraftId = invoice.Id,
            InvoiceNumber = invoiceNumber,
            CustomerName = customerName,
            Status = invoice.Status,
            IssueDate = issueDate == default ? null : issueDate,
            InvoiceTotal = total,
            RiskLevel = riskLevel,
            Risks = risks,
            IsGobdArchived = isGobdArchived,
            IsDatevExported = isDatevExported,
            IsXRechnungReady = isXRechnungReady,
            IsZugferdReady = isXRechnungReady,
            DaysOverdue = daysOverdue,
            ReminderLevel = reminderLevel,
        };
    }

    private static string DetermineRiskLevel(List<string> risks, string status)
    {
        if (status == InvoiceLifecycleStatus.Cancelled) return "none";
        if (risks.Contains("missing_invoice_number") || risks.Contains("missing_issue_date") || risks.Contains("overdue_no_reminder"))
            return "high";
        if (risks.Contains("not_gobd_archived") || risks.Contains("not_datev_exported") || risks.Count >= 3)
            return "medium";
        if (risks.Count > 0)
            return "low";
        return "none";
    }

    private sealed record ReminderSummary(Guid InvoiceDraftId, int MaxLevel, DateTime LastAt);

    private static int RiskOrder(string riskLevel) => riskLevel switch
    {
        "high" => 3,
        "medium" => 2,
        "low" => 1,
        _ => 0,
    };

    private static DateOnly ParseIssueDate(string payloadJson, DateOnly fallback)
    {
        var text = ReadString(payloadJson, "core", "BT-2");
        return DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value) ? value : fallback;
    }

    private static string ReadString(string payloadJson, string section, string property)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            return document.RootElement.TryGetProperty(section, out var sectionElement) &&
                sectionElement.TryGetProperty(property, out var value)
                    ? value.ToString()
                    : "";
        }
        catch (JsonException)
        {
            return "";
        }
    }

    private static decimal ReadDecimal(string payloadJson, string section, string property)
    {
        var text = ReadString(payloadJson, section, property);
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }
}
