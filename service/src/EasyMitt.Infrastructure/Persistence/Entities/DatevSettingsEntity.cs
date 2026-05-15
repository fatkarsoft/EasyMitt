namespace EasyMitt.Infrastructure.Persistence.Entities;

public sealed class DatevSettingsEntity
{
    public Guid CompanyId { get; set; }
    public string ExportFormat { get; set; } = "BasicCsv";
    public string ChartOfAccounts { get; set; } = "SKR03";
    public string RevenueAccount { get; set; } = "8400";
    public string DefaultExpenseAccount { get; set; } = "4980";
    public string CustomerContraAccount { get; set; } = "10000";
    public string VendorContraAccount { get; set; } = "70000";
    public string? ConsultantNumber { get; set; }
    public string? ClientNumber { get; set; }
    public DateOnly FiscalYearStart { get; set; } = new(DateTime.UtcNow.Year, 1, 1);
    public string ExpenseAccountMappingsJson { get; set; } = "[]";
    public string TaxKeyMappingsJson { get; set; } = "[]";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public CompanyEntity? Company { get; set; }
}
