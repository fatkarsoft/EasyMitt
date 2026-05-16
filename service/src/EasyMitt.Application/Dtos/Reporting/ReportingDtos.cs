namespace EasyMitt.Application.Dtos.Reporting;

public sealed class ReportingFilterDto
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
}

public sealed class ReportingSummaryDto
{
    public decimal RevenueNet { get; init; }
    public decimal RevenueTax { get; init; }
    public decimal RevenueGross { get; init; }
    public decimal OpenReceivables { get; init; }
    public decimal CollectedAmount { get; init; }
    public decimal ExpenseTotal { get; init; }
    public decimal NetResult { get; init; }
    public int IssuedInvoiceCount { get; init; }
    public int OverdueInvoiceCount { get; init; }
    public int ExpenseCount { get; init; }
}

public sealed class ReportingRevenuePointDto
{
    public string Period { get; init; } = "";
    public decimal Net { get; init; }
    public decimal Tax { get; init; }
    public decimal Gross { get; init; }
    public int InvoiceCount { get; init; }
}

public sealed class ReportingVatBucketDto
{
    public decimal RatePercent { get; init; }
    public decimal Net { get; init; }
    public decimal Tax { get; init; }
    public decimal Gross { get; init; }
}

public sealed class ReportingAgingBucketDto
{
    public string Bucket { get; init; } = "";
    public int InvoiceCount { get; init; }
    public decimal OpenAmount { get; init; }
}

public sealed class ReportingCustomerPerformanceDto
{
    public Guid? CustomerId { get; init; }
    public string CustomerName { get; init; } = "";
    public decimal RevenueGross { get; init; }
    public decimal OpenAmount { get; init; }
    public int InvoiceCount { get; init; }
}

public sealed class ReportingDatevCoverageDto
{
    public int InvoiceCount { get; init; }
    public int ExportedInvoiceCount { get; init; }
    public decimal CoveragePercent { get; init; }
    public int ExpenseCount { get; init; }
    public int ExportedExpenseCount { get; init; }
    public decimal ExpenseCoveragePercent { get; init; }
}

public sealed class ReportingExpenseCategoryDto
{
    public string Category { get; init; } = "";
    public decimal NetAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public int Count { get; init; }
}

public sealed class ReportingOverviewDto
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public ReportingSummaryDto Summary { get; init; } = new();
    public IReadOnlyList<ReportingRevenuePointDto> RevenueByMonth { get; init; } = Array.Empty<ReportingRevenuePointDto>();
    public IReadOnlyList<ReportingVatBucketDto> VatSummary { get; init; } = Array.Empty<ReportingVatBucketDto>();
    public IReadOnlyList<ReportingAgingBucketDto> Aging { get; init; } = Array.Empty<ReportingAgingBucketDto>();
    public IReadOnlyList<ReportingCustomerPerformanceDto> TopCustomersByRevenue { get; init; } = Array.Empty<ReportingCustomerPerformanceDto>();
    public IReadOnlyList<ReportingCustomerPerformanceDto> TopCustomersByOverdue { get; init; } = Array.Empty<ReportingCustomerPerformanceDto>();
    public ReportingDatevCoverageDto DatevCoverage { get; init; } = new();
    public IReadOnlyList<ReportingExpenseCategoryDto> ExpenseByCategory { get; init; } = Array.Empty<ReportingExpenseCategoryDto>();
}
