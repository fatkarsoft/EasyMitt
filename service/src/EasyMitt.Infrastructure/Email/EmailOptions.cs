namespace EasyMitt.Infrastructure.Email;

public sealed class EmailOptions
{
    public string Backend { get; init; } = "NoOp";

    public string SmtpHost { get; init; } = "";
    public int SmtpPort { get; init; } = 587;
    public string SmtpUser { get; init; } = "";
    public string SmtpPassword { get; init; } = "";
    public bool EnableSsl { get; init; } = true;
    public string FromAddress { get; init; } = "noreply@easymitt.de";
    public string FromName { get; init; } = "EasyMitt";

    public PostmarkEmailOptions Postmark { get; init; } = new();

    public bool IsSmtpConfigured => !string.IsNullOrWhiteSpace(SmtpHost);
}

public sealed class PostmarkEmailOptions
{
    public string ServerToken { get; init; } = "";
    public string MessageStream { get; init; } = "outbound";
}
