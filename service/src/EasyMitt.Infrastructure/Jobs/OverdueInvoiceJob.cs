using System.Globalization;
using System.Text.Json;
using EasyMitt.Domain.Billing;
using EasyMitt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace EasyMitt.Infrastructure.Jobs;

/// <summary>
/// BT-9 vade tarihine göre Issued/Sent fatura → Overdue geçişini otomatik yapar.
/// </summary>
[DisallowConcurrentExecution]
public sealed class OverdueInvoiceJob(
    EasyMittDbContext db,
    JobRunHistory history,
    ILogger<OverdueInvoiceJob> logger) : IJob
{
    public const string Name = "overdue-invoice";

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var candidates = await db.InvoiceDrafts
                .Where(x => x.Status == InvoiceLifecycleStatus.Issued || x.Status == InvoiceLifecycleStatus.Sent)
                .ToListAsync(ct);

            var transitioned = 0;
            foreach (var invoice in candidates)
            {
                var dueDate = ParseDueDate(invoice.PayloadJson);
                if (dueDate is null || dueDate >= today) continue;

                invoice.Status = InvoiceLifecycleStatus.Overdue;
                invoice.UpdatedAtUtc = DateTime.UtcNow;
                transitioned++;
            }

            if (transitioned > 0)
                await db.SaveChangesAsync(ct);

            history.RecordSuccess(Name, DateTime.UtcNow);
            logger.LogInformation("OverdueInvoiceJob {Count} fatura için geçiş yaptı.", transitioned);
        }
        catch (Exception ex)
        {
            history.RecordFailure(Name, DateTime.UtcNow, ex.Message);
            logger.LogError(ex, "OverdueInvoiceJob hata verdi.");
            throw;
        }
    }

    private static DateOnly? ParseDueDate(string payloadJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            // BT-2 issueDate + (default 14 gün) — gerçek BT-9 yoksa.
            if (doc.RootElement.TryGetProperty("core", out var core)
                && core.TryGetProperty("BT-9", out var dueEl)
                && dueEl.ValueKind == JsonValueKind.String
                && DateOnly.TryParse(dueEl.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var due))
            {
                return due;
            }
            if (doc.RootElement.TryGetProperty("core", out var core2)
                && core2.TryGetProperty("BT-2", out var issueEl)
                && issueEl.ValueKind == JsonValueKind.String
                && DateOnly.TryParse(issueEl.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var issue))
            {
                return issue.AddDays(14);
            }
        }
        catch (JsonException) { /* ignore */ }
        return null;
    }
}
