using EasyMitt.Application.Dtos.Datev;

namespace EasyMitt.Application.Abstractions.Persistence;

public interface IDatevExportLogRepository
{
    Task<IReadOnlyList<DatevExportLogDto>> ListAsync(Guid companyId, CancellationToken cancellationToken);

    Task<DatevExportLogDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken);

    Task<DatevExportLogDto?> FindPeriodExportAsync(
        Guid companyId,
        string exportType,
        string? status,
        DateOnly? periodFrom,
        DateOnly? periodTo,
        CancellationToken cancellationToken);

    Task<DatevExportLogDto> CreateAsync(Guid companyId, DatevExportLogCreateDto request, CancellationToken cancellationToken);
}
