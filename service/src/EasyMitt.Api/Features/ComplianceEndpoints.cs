using System.Security.Claims;
using EasyMitt.Api.Responses;
using EasyMitt.Api.Security;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Localization;

namespace EasyMitt.Api.Features;

public static class ComplianceEndpoints
{
    public static void MapComplianceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/compliance").WithTags("Compliance");
        group.MapGet("/dashboard", DashboardAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapGet("/documents/{invoiceDraftId:guid}/timeline", TimelineAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
    }

    private static async Task<IResult> DashboardAsync(
        string? from,
        string? to,
        string? status,
        string? riskLevel,
        ClaimsPrincipal user,
        IComplianceRepository repository,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        DateOnly? parsedFrom = DateOnly.TryParse(from, out var f) ? f : null;
        DateOnly? parsedTo = DateOnly.TryParse(to, out var t) ? t : null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var data = await repository.GetDashboardAsync(user.CompanyId(), today, parsedFrom, parsedTo, status, riskLevel, cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.ComplianceDashboardFound), data));
    }

    private static async Task<IResult> TimelineAsync(
        Guid invoiceDraftId,
        ClaimsPrincipal user,
        IComplianceRepository repository,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var data = await repository.GetDocumentTimelineAsync(user.CompanyId(), invoiceDraftId, cancellationToken);
        return data is null
            ? Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ComplianceDocumentNotFound)))
            : Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.ComplianceTimelineFound), data));
    }
}
