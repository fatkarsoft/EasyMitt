using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Datev;
using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Persistence.Repositories;

public sealed class DatevExportLogRepository(EasyMittDbContext db) : IDatevExportLogRepository
{
    public async Task<IReadOnlyList<DatevExportLogDto>> ListAsync(Guid companyId, CancellationToken cancellationToken) =>
        await db.DatevExportLogs.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(50)
            .Select(x => ToDto(x))
            .ToArrayAsync(cancellationToken);

    public async Task<DatevExportLogDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken) =>
        await db.DatevExportLogs.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Id == id)
            .Select(x => ToDto(x))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<DatevExportLogDto?> FindPeriodExportAsync(
        Guid companyId,
        string exportType,
        string? status,
        DateOnly? periodFrom,
        DateOnly? periodTo,
        CancellationToken cancellationToken)
    {
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        return await db.DatevExportLogs.AsNoTracking()
            .Where(x =>
                x.CompanyId == companyId &&
                x.ExportType == exportType &&
                x.StatusFilter == normalizedStatus &&
                x.PeriodFrom == periodFrom &&
                x.PeriodTo == periodTo &&
                x.ArchiveObjectKey != null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => ToDto(x))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DatevExportLogDto> CreateAsync(Guid companyId, DatevExportLogCreateDto request, CancellationToken cancellationToken)
    {
        var entity = new DatevExportLogEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ExportType = request.ExportType.Trim(),
            StatusFilter = string.IsNullOrWhiteSpace(request.StatusFilter) ? null : request.StatusFilter.Trim(),
            PeriodFrom = request.PeriodFrom,
            PeriodTo = request.PeriodTo,
            FileName = request.FileName.Trim(),
            Sha256Hex = request.Sha256Hex.Trim().ToLowerInvariant(),
            ArchiveObjectKey = string.IsNullOrWhiteSpace(request.ArchiveObjectKey) ? null : request.ArchiveObjectKey.Trim(),
            RowCount = Math.Max(0, request.RowCount),
            WarningCount = Math.Max(0, request.WarningCount),
            TotalAmount = request.TotalAmount,
            TotalTaxAmount = request.TotalTaxAmount,
            UserId = request.UserId,
            UserEmail = request.UserEmail.Trim().ToLowerInvariant(),
            UserDisplayName = request.UserDisplayName.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };

        db.DatevExportLogs.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private static DatevExportLogDto ToDto(DatevExportLogEntity x) => new()
    {
        Id = x.Id,
        ExportType = x.ExportType,
        StatusFilter = x.StatusFilter,
        PeriodFrom = x.PeriodFrom,
        PeriodTo = x.PeriodTo,
        FileName = x.FileName,
        Sha256Hex = x.Sha256Hex,
        ArchiveObjectKey = x.ArchiveObjectKey,
        RowCount = x.RowCount,
        WarningCount = x.WarningCount,
        TotalAmount = x.TotalAmount,
        TotalTaxAmount = x.TotalTaxAmount,
        UserId = x.UserId,
        UserEmail = x.UserEmail,
        UserDisplayName = x.UserDisplayName,
        CreatedAtUtc = x.CreatedAtUtc,
    };
}
