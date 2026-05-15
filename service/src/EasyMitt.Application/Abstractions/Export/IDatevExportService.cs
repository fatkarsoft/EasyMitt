namespace EasyMitt.Application.Abstractions.Export;

public interface IDatevExportService
{
    Task<DatevExportFile> ExportInvoicesAsync(Guid companyId, string? status, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);

    Task<DatevExportFile> ExportExpensesAsync(Guid companyId, string? status, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);

    Task<DatevExportPreviewDto> PreviewInvoicesAsync(Guid companyId, string? status, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);

    Task<DatevExportPreviewDto> PreviewExpensesAsync(Guid companyId, string? status, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);
}

public sealed record DatevExportFile(
    byte[] Content,
    string FileName,
    string ContentType,
    int RowCount,
    int WarningCount,
    decimal TotalAmount,
    decimal TotalTaxAmount);

public sealed class DatevExportPreviewDto
{
    public string ExportFormat { get; init; } = "BasicCsv";
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public IReadOnlyList<DatevExportPreviewRowDto> Rows { get; init; } = Array.Empty<DatevExportPreviewRowDto>();
    public int ReadyCount { get; init; }
    public int WarningCount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal TotalTaxAmount { get; init; }
}

public sealed class DatevExportPreviewRowDto
{
    public DateOnly DocumentDate { get; init; }
    public string DocumentNumber { get; init; } = "";
    public string BookingText { get; init; } = "";
    public string Account { get; init; } = "";
    public string OffsetAccount { get; init; } = "";
    public string DebitCredit { get; init; } = "";
    public decimal Amount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal VatRate { get; init; }
    public string? TaxKey { get; init; }
    public string CurrencyCode { get; init; } = "EUR";
    public string Source { get; init; } = "";
    public string Status { get; init; } = "";
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
