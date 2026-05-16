namespace EasyMitt.Infrastructure.Email;

public sealed class EmailOptions
{
    public string SmtpHost { get; init; } = "";
    public int SmtpPort { get; init; } = 587;
    public string SmtpUser { get; init; } = "";
    public string SmtpPassword { get; init; } = "";
    public bool EnableSsl { get; init; } = true;
    public string FromAddress { get; init; } = "noreply@easymitt.de";
    public string FromName { get; init; } = "EasyMitt";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SmtpHost);
}
