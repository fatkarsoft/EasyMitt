namespace EasyMitt.Infrastructure.Ingestion;

public sealed class ScanServiceOptions
{
    public string BaseUrl { get; init; } = "http://127.0.0.1:7332";

    public int TimeoutSeconds { get; init; } = 120;

    public int MaxFileBytes { get; init; } = 8 * 1024 * 1024;
}
