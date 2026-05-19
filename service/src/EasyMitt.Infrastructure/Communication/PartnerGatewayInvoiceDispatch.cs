using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EasyMitt.Application.Abstractions.Communication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyMitt.Infrastructure.Communication;

/// <summary>
/// Bir partner Peppol AP geçidine HTTP POST yapan dispatch implementasyonu.
/// Gateway sözleşmesi: { senderId, recipientId, contentType, payloadBase64 } gönderir; { dispatchId, status } döner.
/// </summary>
public sealed class PartnerGatewayInvoiceDispatch(
    HttpClient httpClient,
    IOptions<DispatchOptions> optionsAccessor,
    ILogger<PartnerGatewayInvoiceDispatch> logger) : IInvoiceDispatch
{
    private readonly DispatchOptions _options = optionsAccessor.Value;

    public async Task<InvoiceDispatchReceipt> SubmitAsync(InvoiceDispatchRequest request, CancellationToken cancellationToken)
    {
        if (!_options.PartnerGateway.IsConfigured)
        {
            logger.LogWarning("PartnerGateway baseUrl tanımlı değil; dispatch atlandı.");
            return new InvoiceDispatchReceipt($"unconfigured-{Guid.NewGuid():N}", "skipped_unconfigured", null);
        }

        var body = new
        {
            senderId = _options.PartnerGateway.ParticipantId,
            recipientId = request.RecipientEndpointId,
            contentType = request.PayloadContentType,
            payloadBase64 = Convert.ToBase64String(request.Payload),
        };

        var url = _options.PartnerGateway.BaseUrl.TrimEnd('/') + "/invoices";

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(body),
            };
            if (!string.IsNullOrWhiteSpace(_options.PartnerGateway.ApiKey))
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.PartnerGateway.ApiKey);
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("PartnerGateway dispatch başarısız: {Status} {Body}", (int)response.StatusCode, raw);
                return new InvoiceDispatchReceipt(
                    $"failed-{Guid.NewGuid():N}",
                    $"failed_{(int)response.StatusCode}",
                    new Dictionary<string, string> { ["raw"] = Truncate(raw, 1024) });
            }

            string dispatchId = $"partner-{Guid.NewGuid():N}";
            string status = "accepted";
            try
            {
                using var json = JsonDocument.Parse(raw);
                if (json.RootElement.TryGetProperty("dispatchId", out var dispatchIdEl) && dispatchIdEl.ValueKind == JsonValueKind.String)
                    dispatchId = dispatchIdEl.GetString() ?? dispatchId;
                if (json.RootElement.TryGetProperty("status", out var statusEl) && statusEl.ValueKind == JsonValueKind.String)
                    status = statusEl.GetString() ?? status;
            }
            catch (JsonException) { /* keep defaults */ }

            return new InvoiceDispatchReceipt(dispatchId, status, new Dictionary<string, string>
            {
                ["partnerId"] = _options.PartnerGateway.ParticipantId,
                ["raw"] = Truncate(raw, 2048),
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PartnerGateway dispatch sırasında beklenmeyen hata.");
            return new InvoiceDispatchReceipt(
                $"errored-{Guid.NewGuid():N}",
                "errored",
                new Dictionary<string, string> { ["error"] = Truncate(ex.Message, 512) });
        }
    }

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= max ? value : value[..max];
    }
}
