using System.Security.Claims;
using EasyMitt.Api.Responses;
using EasyMitt.Api.Security;
using EasyMitt.Application.Abstractions.Archiving;
using EasyMitt.Application.Abstractions.Jobs;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Localization;

namespace EasyMitt.Api.Features;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/admin").WithTags("Admin");

        group.MapGet("/jobs", ListJobsAsync).RequireAuthorization(AuthorizationPolicies.AdminOnly);
        group.MapPost("/jobs/{name}/run", RunJobAsync).RequireAuthorization(AuthorizationPolicies.AdminOnly);
    }

    private static IResult ListJobsAsync(
        IJobRegistry registry,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext)
    {
        var jobs = registry.List();
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.JobsFound), jobs));
    }

    private static async Task<IResult> RunJobAsync(
        string name,
        IJobRegistry registry,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await registry.RunNowAsync(name, cancellationToken);
        if (result is null)
            return Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.JobNotFound)));
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.JobTriggered), result));
    }
}

public static class ComplianceVerifyEndpoints
{
    public static void MapComplianceVerifyEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/compliance/verify-archive/{invoiceId:guid}", VerifyAsync)
            .WithTags("Compliance")
            .RequireAuthorization(AuthorizationPolicies.AdminOnly);
    }

    private static async Task<IResult> VerifyAsync(
        Guid invoiceId,
        ClaimsPrincipal user,
        IArchiveVerifier verifier,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await verifier.VerifyInvoiceAsync(user.CompanyId(), invoiceId, cancellationToken);
        if (!result.Found)
            return Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ArchiveNotFound)));

        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(result.HashMatches ? MessageKeys.ArchiveVerified : MessageKeys.ArchiveHashMismatch), result));
    }
}
