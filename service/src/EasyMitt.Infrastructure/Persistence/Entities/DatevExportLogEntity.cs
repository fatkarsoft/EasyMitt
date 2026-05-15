namespace EasyMitt.Infrastructure.Persistence.Entities;

public sealed class DatevExportLogEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string ExportType { get; set; } = "";
    public string? StatusFilter { get; set; }
    public DateOnly? PeriodFrom { get; set; }
    public DateOnly? PeriodTo { get; set; }
    public string FileName { get; set; } = "";
    public string Sha256Hex { get; set; } = "";
    public string? ArchiveObjectKey { get; set; }
    public int RowCount { get; set; }
    public int WarningCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = "";
    public string UserDisplayName { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }

    public CompanyEntity? Company { get; set; }
}
