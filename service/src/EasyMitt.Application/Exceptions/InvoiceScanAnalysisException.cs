namespace EasyMitt.Application.Exceptions;

public sealed class InvoiceScanAnalysisException(string reason, string? detail = null)
    : Exception(reason)
{
    public string Reason { get; } = reason;

    public string? Detail { get; } = detail;
}
