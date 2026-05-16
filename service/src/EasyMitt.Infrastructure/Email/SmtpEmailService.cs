using System.Net;
using System.Net.Mail;
using EasyMitt.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyMitt.Infrastructure.Email;

public sealed class SmtpEmailService(IOptions<EmailOptions> optionsAccessor, ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = optionsAccessor.Value;

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        try
        {
            using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                EnableSsl = _options.EnableSsl,
                Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPassword),
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = false,
            };
            mail.To.Add(message.ToEmail);

            if (message.AttachmentBytes is { Length: > 0 } && !string.IsNullOrEmpty(message.AttachmentFileName))
            {
                var stream = new MemoryStream(message.AttachmentBytes);
                var attachment = new Attachment(stream, message.AttachmentFileName, message.AttachmentMimeType ?? "application/octet-stream");
                mail.Attachments.Add(attachment);
            }

            await client.SendMailAsync(mail, ct);
            logger.LogInformation("Email sent to {ToEmail} subject={Subject}", message.ToEmail, message.Subject);
            return new EmailSendResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {ToEmail}", message.ToEmail);
            return new EmailSendResult(false, ex.Message);
        }
    }
}
