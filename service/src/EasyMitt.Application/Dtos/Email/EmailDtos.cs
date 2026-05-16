namespace EasyMitt.Application.Dtos.Email;

public sealed record SendInvoiceEmailRequestDto(
    string ToEmail,
    string Subject,
    string Body);

public sealed record SendQuoteEmailRequestDto(
    string ToEmail,
    string Subject,
    string Body);

public sealed record SendDunningEmailRequestDto(
    Guid DunningReminderId,
    string ToEmail,
    string Subject,
    string Body);

public sealed record EmailDeliveryLogDto(
    Guid Id,
    string DocumentType,
    Guid DocumentId,
    string ToEmail,
    string Subject,
    string AttachmentType,
    string Status,
    string? ErrorMessage,
    string SenderUserEmail,
    DateTime CreatedAtUtc);
