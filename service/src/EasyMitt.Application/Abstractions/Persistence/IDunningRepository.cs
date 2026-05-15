using EasyMitt.Application.Dtos.Dunning;

namespace EasyMitt.Application.Abstractions.Persistence;

public interface IDunningRepository
{
    Task<DunningOverviewDto> GetOverviewAsync(Guid companyId, DateOnly today, CancellationToken cancellationToken);

    Task<IReadOnlyList<DunningReminderDto>> GetInvoiceRemindersAsync(Guid companyId, Guid invoiceDraftId, CancellationToken cancellationToken);

    Task<DunningReminderDto?> CreateReminderAsync(Guid companyId, Guid userId, string userEmail, DunningReminderCreateDto request, CancellationToken cancellationToken);
}
