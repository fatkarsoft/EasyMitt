using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EasyMitt.Application.Abstractions.Transformation;
using EasyMitt.Application.Dtos.Ingestion;
using EasyMitt.Application.Exceptions;
using Microsoft.Extensions.Options;

namespace EasyMitt.Infrastructure.Ingestion;

public sealed class ScanServiceInvoiceScanAnalyzer(
    HttpClient httpClient,
    IOptions<ScanServiceOptions> options)
    : IScannedInvoiceImportAnalyzer
{
    private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    public async Task<RawInvoiceImportDto> AnalyzeAsync(
        Stream file,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        var scanOptions = options.Value;
        if (string.IsNullOrWhiteSpace(scanOptions.BaseUrl))
        {
            throw new InvalidOperationException("scan_not_configured");
        }

        if (!SupportedContentTypes.Contains(contentType))
        {
            throw new NotSupportedException("unsupported_scan_file");
        }

        if (file.Length == 0 || file.Length > scanOptions.MaxFileBytes)
        {
            throw new InvalidOperationException("invalid_scan_file");
        }

        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(file);
        fileContent.Headers.ContentType = new(contentType);
        form.Add(fileContent, "file", string.IsNullOrWhiteSpace(fileName) ? "invoice-image" : fileName);

        HttpResponseMessage response;
        string responseJson;
        try
        {
            response = await httpClient.PostAsync("/api/scan/invoice", form, cancellationToken);
            responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new InvoiceScanAnalysisException("scan_service_unavailable", ex.Message);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvoiceScanAnalysisException("scan_service_timeout", ex.Message);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvoiceScanAnalysisException(
                $"scan_service_failed:{response.StatusCode}",
                TrimDetail(responseJson));
        }

        var envelope = JsonSerializer.Deserialize<ScanServiceResponse>(responseJson, JsonOptions);
        if (envelope is null)
        {
            throw new InvoiceScanAnalysisException("scan_service_invalid_response", TrimDetail(responseJson));
        }

        if (!envelope.Success)
        {
            throw new InvoiceScanAnalysisException(
                envelope.Error?.Code ?? "scan_service_failed",
                TrimDetail(envelope.Error?.Message ?? responseJson));
        }

        if (envelope.Data is null)
        {
            throw new InvoiceScanAnalysisException("scan_service_empty_response", TrimDetail(responseJson));
        }

        return envelope.Data;
    }

    private static string TrimDetail(string detail) =>
        detail.Length <= 1200 ? detail : detail[..1200];

    private sealed record ScanServiceResponse(
        bool Success,
        RawInvoiceImportDto? Data,
        ScanServiceError? Error);

    private sealed record ScanServiceError(string Code, string Message);
}
