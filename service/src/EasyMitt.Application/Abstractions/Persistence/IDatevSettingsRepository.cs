using EasyMitt.Application.Dtos.Datev;

namespace EasyMitt.Application.Abstractions.Persistence;

public interface IDatevSettingsRepository
{
    Task<DatevSettingsDto> GetAsync(Guid companyId, CancellationToken cancellationToken);

    Task<DatevSettingsDto> UpsertAsync(Guid companyId, DatevSettingsUpsertDto request, CancellationToken cancellationToken);
}
