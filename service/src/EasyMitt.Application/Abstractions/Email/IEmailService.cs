namespace EasyMitt.Application.Abstractions.Email;

public interface IEmailService
{
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default);
}

public sealed record EmailMessage(
    string ToEmail,
    string Subject,
    string Body,
    string? AttachmentFileName = null,
    byte[]? AttachmentBytes = null,
    string? AttachmentMimeType = null);

public sealed record EmailSendResult(bool Success, string? ErrorMessage = null);
