namespace EasyMitt.Infrastructure.Jobs;

public sealed class JobOptions
{
    public bool EmailRetryEnabled { get; init; } = true;
    public bool OverdueInvoiceEnabled { get; init; } = true;
    public bool DatevExportScheduledEnabled { get; init; } = false;
    public string DatevExportCron { get; init; } = "0 0 3 1 * ?";
}
