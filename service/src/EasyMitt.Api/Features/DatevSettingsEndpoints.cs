using System.Security.Claims;
using EasyMitt.Api.Responses;
using EasyMitt.Api.Security;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Datev;
using EasyMitt.Application.Localization;
using FluentValidation;

namespace EasyMitt.Api.Features;

public static class DatevSettingsEndpoints
{
    public static void MapDatevSettingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/datev/settings").WithTags("DATEV");
        group.MapGet("/", GetAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapPut("/", UpdateAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
    }

    private static async Task<IResult> GetAsync(
        ClaimsPrincipal user,
        IDatevSettingsRepository repository,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var data = await repository.GetAsync(user.CompanyId(), cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.SystemHealthy), data));
    }

    private static async Task<IResult> UpdateAsync(
        DatevSettingsUpsertDto body,
        ClaimsPrincipal user,
        IDatevSettingsRepository repository,
        IValidator<DatevSettingsUpsertDto> validator,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(body, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.BadRequest(responseFactory.Failure(
                httpContext,
                localizer.Get(MessageKeys.ValidationFailed),
                EndpointHelpers.ToErrorDictionary(validation.Errors, localizer)));
        }

        var data = await repository.UpsertAsync(user.CompanyId(), body, cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.SystemHealthy), data));
    }
}
