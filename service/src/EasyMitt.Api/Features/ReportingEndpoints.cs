using System.Security.Claims;
using EasyMitt.Api.Responses;
using EasyMitt.Api.Security;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Localization;

namespace EasyMitt.Api.Features;

public static class ReportingEndpoints
{
    public static void MapReportingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/reporting").WithTags("Reporting");
        group.MapGet("/overview", OverviewAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
    }

    private static async Task<IResult> OverviewAsync(
        DateOnly? from,
        DateOnly? to,
        ClaimsPrincipal user,
        IReportingRepository repository,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rangeTo = to ?? today;
        var rangeFrom = from ?? new DateOnly(rangeTo.Year, 1, 1);
        if (rangeFrom > rangeTo)
        {
            (rangeFrom, rangeTo) = (rangeTo, rangeFrom);
        }

        var data = await repository.GetOverviewAsync(user.CompanyId(), rangeFrom, rangeTo, today, cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.ReportingOverviewFound), data));
    }
}
