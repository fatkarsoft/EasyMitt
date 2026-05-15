using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using EasyMitt.Api.Responses;
using EasyMitt.Api.Security;
using EasyMitt.Application.Abstractions.Archiving;
using EasyMitt.Application.Abstractions.Export;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Datev;
using EasyMitt.Application.Localization;

namespace EasyMitt.Api.Features;

public static class DatevEndpoints
{
    public static void MapDatevEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/datev").WithTags("DATEV");

        group.MapGet("/invoices.csv", ExportInvoicesAsync)
            .RequireAuthorization(AuthorizationPolicies.InvoiceRead);

        group.MapGet("/expenses.csv", ExportExpensesAsync)
            .RequireAuthorization(AuthorizationPolicies.InvoiceRead);

        group.MapGet("/invoices/preview", PreviewInvoicesAsync)
            .RequireAuthorization(AuthorizationPolicies.InvoiceRead);

        group.MapGet("/expenses/preview", PreviewExpensesAsync)
            .RequireAuthorization(AuthorizationPolicies.InvoiceRead);

        group.MapGet("/exports", ListExportsAsync)
            .RequireAuthorization(AuthorizationPolicies.InvoiceRead);

        group.MapGet("/exports/{id:guid}/download", DownloadExportAsync)
            .RequireAuthorization(AuthorizationPolicies.InvoiceRead);
    }

    private static async Task<IResult> ExportInvoicesAsync(
        string? status,
        DateOnly? from,
        DateOnly? to,
        bool? force,
        ClaimsPrincipal user,
        IDatevExportService exportService,
        IDatevExportLogRepository exportLogRepository,
        IImmutableArchiveStore archiveStore,
        CancellationToken cancellationToken)
    {
        if (force is not true)
        {
            var existing = await exportLogRepository.FindPeriodExportAsync(user.CompanyId(), "Invoices", status, from, to, cancellationToken);
            var archivedFile = await TryReadArchivedExportAsync(existing, archiveStore, cancellationToken);
            if (archivedFile is not null)
            {
                return archivedFile;
            }
        }

        var file = await exportService.ExportInvoicesAsync(user.CompanyId(), status, from, to, cancellationToken);
        await LogExportAsync("Invoices", status, from, to, file, user, exportLogRepository, archiveStore, cancellationToken);
        return Results.File(file.Content, file.ContentType, file.FileName);
    }

    private static async Task<IResult> ExportExpensesAsync(
        string? status,
        DateOnly? from,
        DateOnly? to,
        bool? force,
        ClaimsPrincipal user,
        IDatevExportService exportService,
        IDatevExportLogRepository exportLogRepository,
        IImmutableArchiveStore archiveStore,
        CancellationToken cancellationToken)
    {
        if (force is not true)
        {
            var existing = await exportLogRepository.FindPeriodExportAsync(user.CompanyId(), "Expenses", status, from, to, cancellationToken);
            var archivedFile = await TryReadArchivedExportAsync(existing, archiveStore, cancellationToken);
            if (archivedFile is not null)
            {
                return archivedFile;
            }
        }

        var file = await exportService.ExportExpensesAsync(user.CompanyId(), status, from, to, cancellationToken);
        await LogExportAsync("Expenses", status, from, to, file, user, exportLogRepository, archiveStore, cancellationToken);
        return Results.File(file.Content, file.ContentType, file.FileName);
    }

    private static async Task<IResult> PreviewInvoicesAsync(
        string? status,
        DateOnly? from,
        DateOnly? to,
        ClaimsPrincipal user,
        IDatevExportService exportService,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var data = await exportService.PreviewInvoicesAsync(user.CompanyId(), status, from, to, cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.SystemHealthy), data));
    }

    private static async Task<IResult> PreviewExpensesAsync(
        string? status,
        DateOnly? from,
        DateOnly? to,
        ClaimsPrincipal user,
        IDatevExportService exportService,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var data = await exportService.PreviewExpensesAsync(user.CompanyId(), status, from, to, cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.SystemHealthy), data));
    }

    private static async Task<IResult> ListExportsAsync(
        ClaimsPrincipal user,
        IDatevExportLogRepository exportLogRepository,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var data = await exportLogRepository.ListAsync(user.CompanyId(), cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.SystemHealthy), data));
    }

    private static async Task<IResult> DownloadExportAsync(
        Guid id,
        ClaimsPrincipal user,
        IDatevExportLogRepository exportLogRepository,
        IImmutableArchiveStore archiveStore,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var log = await exportLogRepository.GetAsync(user.CompanyId(), id, cancellationToken);
        if (log is null || string.IsNullOrWhiteSpace(log.ArchiveObjectKey))
        {
            return Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.InvoiceDraftNotFound)));
        }

        var content = await archiveStore.ReadAsync(log.ArchiveObjectKey, cancellationToken);
        if (content is null)
        {
            return Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.InvoiceDraftNotFound)));
        }

        if (!Sha256Hex(content).Equals(log.Sha256Hex, StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ValidationFailed)));
        }

        return Results.File(content, "text/csv; charset=utf-8", log.FileName);
    }

    private static async Task LogExportAsync(
        string exportType,
        string? status,
        DateOnly? from,
        DateOnly? to,
        DatevExportFile file,
        ClaimsPrincipal user,
        IDatevExportLogRepository exportLogRepository,
        IImmutableArchiveStore archiveStore,
        CancellationToken cancellationToken)
    {
        var sha256Hex = Sha256Hex(file.Content);
        var archive = await archiveStore.WriteAsync(file.Content, sha256Hex, cancellationToken, ".csv");

        await exportLogRepository.CreateAsync(
            user.CompanyId(),
            new DatevExportLogCreateDto
            {
                ExportType = exportType,
                StatusFilter = status,
                PeriodFrom = from,
                PeriodTo = to,
                FileName = file.FileName,
                Sha256Hex = sha256Hex,
                ArchiveObjectKey = archive.ObjectKey,
                RowCount = file.RowCount,
                WarningCount = file.WarningCount,
                TotalAmount = file.TotalAmount,
                TotalTaxAmount = file.TotalTaxAmount,
                UserId = user.UserId(),
                UserEmail = user.FindFirstValue(ClaimTypes.Email) ?? "",
                UserDisplayName = user.FindFirstValue(ClaimTypes.Name) ?? "",
            },
            cancellationToken);
    }

    private static string Sha256Hex(byte[] content)
    {
        var hash = SHA256.HashData(content);
        var builder = new StringBuilder(hash.Length * 2);
        foreach (var item in hash)
        {
            builder.Append(item.ToString("x2"));
        }

        return builder.ToString();
    }

    private static async Task<IResult?> TryReadArchivedExportAsync(
        DatevExportLogDto? log,
        IImmutableArchiveStore archiveStore,
        CancellationToken cancellationToken)
    {
        if (log is null || string.IsNullOrWhiteSpace(log.ArchiveObjectKey))
        {
            return null;
        }

        var content = await archiveStore.ReadAsync(log.ArchiveObjectKey, cancellationToken);
        if (content is null || !Sha256Hex(content).Equals(log.Sha256Hex, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Results.File(content, "text/csv; charset=utf-8", log.FileName);
    }
}
