using EasyMitt.Application.Dtos.Reporting;

namespace EasyMitt.Application.Abstractions.Persistence;

public interface IReportingRepository
{
    Task<ReportingOverviewDto> GetOverviewAsync(Guid companyId, DateOnly from, DateOnly to, DateOnly today, CancellationToken cancellationToken);
}
