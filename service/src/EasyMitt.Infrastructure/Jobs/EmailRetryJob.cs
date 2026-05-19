using EasyMitt.Application.Abstractions.Email;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Email;
using EasyMitt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace EasyMitt.Infrastructure.Jobs;

/// <summary>
/// Son 24 saatte Failed durumdaki email_delivery_logs için 3 retry (exp backoff).
/// </summary>
[DisallowConcurrentExecution]
public sealed class EmailRetryJob(
    EasyMittDbContext db,
    IEmailService emailService,
    IEmailDeliveryLogRepository logRepository,
    JobRunHistory history,
    ILogger<EmailRetryJob> logger) : IJob
{
    public const string Name = "email-retry";

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var since = DateTime.UtcNow.AddHours(-24);
        try
        {
            var failed = await db.EmailDeliveryLogs.AsNoTracking()
                .Where(x => x.Status == "Failed" && x.CreatedAtUtc >= since)
                .OrderBy(x => x.CreatedAtUtc)
                .Take(50)
                .ToListAsync(ct);

            foreach (var entry in failed)
            {
                var key = entry.Id.ToString();
                var attempts = entry.ErrorMessage?.Contains("retry=") == true ? 1 : 0;
                if (attempts >= 3) continue;

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempts));
                await Task.Delay(delay, ct);

                var result = await emailService.SendAsync(new EmailMessage(
                    entry.ToEmail,
                    entry.Subject,
                    "[Retry] " + entry.Subject,
                    null,
                    null,
                    null), ct);

                var status = result.Success ? "Sent" : "Failed";
                await logRepository.AddAsync(new EmailDeliveryLogEntry(
                    entry.CompanyId,
                    entry.DocumentType,
                    entry.DocumentId,
                    entry.ToEmail,
                    entry.Subject,
                    entry.AttachmentType,
                    status,
                    $"retry={attempts + 1};{result.ErrorMessage}",
                    entry.SenderUserId,
                    entry.SenderUserEmail), ct);
            }

            history.RecordSuccess(Name, DateTime.UtcNow);
            logger.LogInformation("EmailRetryJob {Count} log üzerinden çalıştı.", failed.Count);
        }
        catch (Exception ex)
        {
            history.RecordFailure(Name, DateTime.UtcNow, ex.Message);
            logger.LogError(ex, "EmailRetryJob hata verdi.");
            throw;
        }
    }
}
