namespace EasyMitt.Application.Dtos.Datev;

public sealed class DatevExportLogDto
{
    public Guid Id { get; init; }
    public string ExportType { get; init; } = "";
    public string? StatusFilter { get; init; }
    public DateOnly? PeriodFrom { get; init; }
    public DateOnly? PeriodTo { get; init; }
    public string FileName { get; init; } = "";
    public string Sha256Hex { get; init; } = "";
    public string? ArchiveObjectKey { get; init; }
    public int RowCount { get; init; }
    public int WarningCount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal TotalTaxAmount { get; init; }
    public Guid UserId { get; init; }
    public string UserEmail { get; init; } = "";
    public string UserDisplayName { get; init; } = "";
    public DateTime CreatedAtUtc { get; init; }
}

public sealed class DatevExportLogCreateDto
{
    public string ExportType { get; init; } = "";
    public string? StatusFilter { get; init; }
    public DateOnly? PeriodFrom { get; init; }
    public DateOnly? PeriodTo { get; init; }
    public string FileName { get; init; } = "";
    public string Sha256Hex { get; init; } = "";
    public string? ArchiveObjectKey { get; init; }
    public int RowCount { get; init; }
    public int WarningCount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal TotalTaxAmount { get; init; }
    public Guid UserId { get; init; }
    public string UserEmail { get; init; } = "";
    public string UserDisplayName { get; init; } = "";
}
