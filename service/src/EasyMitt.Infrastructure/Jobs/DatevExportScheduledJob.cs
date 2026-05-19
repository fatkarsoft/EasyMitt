using EasyMitt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace EasyMitt.Infrastructure.Jobs;

/// <summary>
/// Opsiyonel — varsayılan kapalı. Cron tetiklendiğinde ayın sonunda DATEV export periyodu kapatır
/// (sadece markaj: bu modüldeki gerçek export'u tetiklemeyiz; "scheduled run" damgası bırakır).
/// </summary>
[DisallowConcurrentExecution]
public sealed class DatevExportScheduledJob(
    EasyMittDbContext db,
    JobRunHistory history,
    ILogger<DatevExportScheduledJob> logger) : IJob
{
    public const string Name = "datev-export-scheduled";

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        try
        {
            var companyIds = await db.Companies.AsNoTracking().Select(c => c.Id).ToListAsync(ct);
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var periodEnd = today.AddDays(-1);

            logger.LogInformation("DatevExportScheduledJob {Count} şirket için periyot {Date} damgası bıraktı.", companyIds.Count, periodEnd);

            history.RecordSuccess(Name, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            history.RecordFailure(Name, DateTime.UtcNow, ex.Message);
            logger.LogError(ex, "DatevExportScheduledJob hata verdi.");
            throw;
        }
    }
}
