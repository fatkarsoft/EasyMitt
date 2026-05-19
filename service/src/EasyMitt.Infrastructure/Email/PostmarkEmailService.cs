using System.Net.Http.Headers;
using System.Net.Http.Json;
using EasyMitt.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyMitt.Infrastructure.Email;

public sealed class PostmarkEmailService(
    HttpClient httpClient,
    IOptions<EmailOptions> optionsAccessor,
    ILogger<PostmarkEmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = optionsAccessor.Value;

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Postmark.ServerToken))
        {
            logger.LogWarning("Postmark server token tanımlı değil; e-posta gönderilemedi.");
            return new EmailSendResult(false, "postmark_not_configured");
        }

        try
        {
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!httpClient.DefaultRequestHeaders.Contains("X-Postmark-Server-Token"))
                httpClient.DefaultRequestHeaders.Add("X-Postmark-Server-Token", _options.Postmark.ServerToken);

            var body = new Dictionary<string, object?>
            {
                ["From"] = string.IsNullOrWhiteSpace(_options.FromName)
                    ? _options.FromAddress
                    : $"{_options.FromName} <{_options.FromAddress}>",
                ["To"] = message.ToEmail,
                ["Subject"] = message.Subject,
                ["TextBody"] = message.Body,
                ["MessageStream"] = string.IsNullOrWhiteSpace(_options.Postmark.MessageStream) ? "outbound" : _options.Postmark.MessageStream,
            };

            if (message.AttachmentBytes is { Length: > 0 } && !string.IsNullOrEmpty(message.AttachmentFileName))
            {
                body["Attachments"] = new[]
                {
                    new
                    {
                        Name = message.AttachmentFileName,
                        Content = Convert.ToBase64String(message.AttachmentBytes),
                        ContentType = message.AttachmentMimeType ?? "application/octet-stream",
                    },
                };
            }

            using var response = await httpClient.PostAsJsonAsync("https://api.postmarkapp.com/email", body, ct);
            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Postmark gönderimi başarısız oldu: {StatusCode} {Detail}", (int)response.StatusCode, detail);
                return new EmailSendResult(false, $"postmark_{(int)response.StatusCode}");
            }

            logger.LogInformation("Postmark email sent to {ToEmail} subject={Subject}", message.ToEmail, message.Subject);
            return new EmailSendResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Postmark gönderimi sırasında beklenmeyen hata.");
            return new EmailSendResult(false, ex.Message);
        }
    }
}
