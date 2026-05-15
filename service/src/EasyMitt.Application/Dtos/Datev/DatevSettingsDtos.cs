namespace EasyMitt.Application.Dtos.Datev;

public sealed class DatevSettingsDto
{
    public string ExportFormat { get; init; } = "BasicCsv";
    public string ChartOfAccounts { get; init; } = "SKR03";
    public string RevenueAccount { get; init; } = "8400";
    public string DefaultExpenseAccount { get; init; } = "4980";
    public string CustomerContraAccount { get; init; } = "10000";
    public string VendorContraAccount { get; init; } = "70000";
    public string? ConsultantNumber { get; init; }
    public string? ClientNumber { get; init; }
    public DateOnly FiscalYearStart { get; init; } = new(DateTime.UtcNow.Year, 1, 1);
    public IReadOnlyList<DatevExpenseAccountMappingDto> ExpenseAccountMappings { get; init; } = Array.Empty<DatevExpenseAccountMappingDto>();
    public IReadOnlyList<DatevTaxKeyMappingDto> TaxKeyMappings { get; init; } = Array.Empty<DatevTaxKeyMappingDto>();
    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class DatevSettingsUpsertDto
{
    public string ExportFormat { get; init; } = "BasicCsv";
    public string ChartOfAccounts { get; init; } = "SKR03";
    public string RevenueAccount { get; init; } = "8400";
    public string DefaultExpenseAccount { get; init; } = "4980";
    public string CustomerContraAccount { get; init; } = "10000";
    public string VendorContraAccount { get; init; } = "70000";
    public string? ConsultantNumber { get; init; }
    public string? ClientNumber { get; init; }
    public DateOnly FiscalYearStart { get; init; } = new(DateTime.UtcNow.Year, 1, 1);
    public IReadOnlyList<DatevExpenseAccountMappingDto> ExpenseAccountMappings { get; init; } = Array.Empty<DatevExpenseAccountMappingDto>();
    public IReadOnlyList<DatevTaxKeyMappingDto> TaxKeyMappings { get; init; } = Array.Empty<DatevTaxKeyMappingDto>();
}

public sealed class DatevExpenseAccountMappingDto
{
    public string Category { get; init; } = "";
    public string Account { get; init; } = "";
}

public sealed class DatevTaxKeyMappingDto
{
    public string Source { get; init; } = "";
    public decimal VatRate { get; init; }
    public string TaxKey { get; init; } = "";
}
