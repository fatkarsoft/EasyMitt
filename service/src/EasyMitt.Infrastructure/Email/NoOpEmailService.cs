using EasyMitt.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;

namespace EasyMitt.Infrastructure.Email;

public sealed class NoOpEmailService(ILogger<NoOpEmailService> logger) : IEmailService
{
    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        logger.LogWarning("NoOpEmailService: SMTP not configured. Would have sent to {ToEmail} subject={Subject}", message.ToEmail, message.Subject);
        return Task.FromResult(new EmailSendResult(true));
    }
}
