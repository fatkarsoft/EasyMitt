using System.Security.Claims;
using EasyMitt.Api.Responses;
using EasyMitt.Api.Security;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Dunning;
using EasyMitt.Application.Localization;

namespace EasyMitt.Api.Features;

public static class DunningEndpoints
{
    public static void MapDunningEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/dunning").WithTags("Dunning");
        group.MapGet("/overview", OverviewAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapGet("/invoices/{invoiceId:guid}/reminders", InvoiceRemindersAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapPost("/reminders", CreateReminderAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
    }

    private static async Task<IResult> OverviewAsync(ClaimsPrincipal user, IDunningRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var data = await repository.GetOverviewAsync(user.CompanyId(), DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.DunningFound), data));
    }

    private static async Task<IResult> InvoiceRemindersAsync(Guid invoiceId, ClaimsPrincipal user, IDunningRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var data = await repository.GetInvoiceRemindersAsync(user.CompanyId(), invoiceId, cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.DunningFound), data));
    }

    private static async Task<IResult> CreateReminderAsync(DunningReminderCreateDto body, ClaimsPrincipal user, IDunningRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken)
    {
        try
        {
            var data = await repository.CreateReminderAsync(
                user.CompanyId(),
                user.UserId(),
                user.FindFirstValue(ClaimTypes.Email) ?? "",
                body,
                cancellationToken);
            return data is null
                ? Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.InvoiceDraftNotFound)))
                : Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.DunningReminderCreated), data));
        }
        catch (InvalidOperationException)
        {
            return Results.BadRequest(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ValidationFailed)));
        }
    }
}
