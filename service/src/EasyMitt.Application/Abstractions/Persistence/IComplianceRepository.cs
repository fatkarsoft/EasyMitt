using EasyMitt.Application.Dtos.Compliance;

namespace EasyMitt.Application.Abstractions.Persistence;

public interface IComplianceRepository
{
    Task<ComplianceDashboardDto> GetDashboardAsync(
        Guid companyId,
        DateOnly today,
        DateOnly? from,
        DateOnly? to,
        string? status,
        string? riskLevel,
        CancellationToken cancellationToken);

    Task<ComplianceDocumentTimelineDto?> GetDocumentTimelineAsync(
        Guid companyId,
        Guid invoiceDraftId,
        CancellationToken cancellationToken);
}
