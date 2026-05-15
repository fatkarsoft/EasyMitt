using System.Security.Claims;
using EasyMitt.Api.Responses;
using EasyMitt.Api.Security;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Quotes;
using EasyMitt.Application.Localization;
using EasyMitt.Application.Services.Billing;
using EasyMitt.Domain.Quotes;
using FluentValidation;

namespace EasyMitt.Api.Features;

public static class QuoteEndpoints
{
    public static void MapQuoteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/quotes").WithTags("Quotes");
        group.MapGet("/", SearchAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapGet("/{id:guid}", GetAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapPost("/", CreateAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPut("/{id:guid}", UpdateAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/{id:guid}/send", (Guid id, ClaimsPrincipal user, IQuoteRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken) =>
            UpdateStatusAsync(id, QuoteStatus.Sent, user, repository, responseFactory, localizer, httpContext, cancellationToken)).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/{id:guid}/accept", (Guid id, ClaimsPrincipal user, IQuoteRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken) =>
            UpdateStatusAsync(id, QuoteStatus.Accepted, user, repository, responseFactory, localizer, httpContext, cancellationToken)).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/{id:guid}/decline", (Guid id, ClaimsPrincipal user, IQuoteRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken) =>
            UpdateStatusAsync(id, QuoteStatus.Declined, user, repository, responseFactory, localizer, httpContext, cancellationToken)).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/{id:guid}/convert-to-invoice", ConvertToInvoiceAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
    }

    private static async Task<IResult> SearchAsync(
        string? q,
        string? status,
        ClaimsPrincipal user,
        IQuoteRepository repository,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var data = await repository.SearchAsync(user.CompanyId(), q, status, cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.QuotesFound), data));
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ClaimsPrincipal user,
        IQuoteRepository repository,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var data = await repository.GetAsync(user.CompanyId(), id, cancellationToken);
        return data is null
            ? Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.QuoteNotFound)))
            : Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.QuotesFound), data));
    }

    private static async Task<IResult> CreateAsync(
        QuoteUpsertDto body,
        ClaimsPrincipal user,
        IValidator<QuoteUpsertDto> validator,
        IQuoteRepository repository,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(body, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.BadRequest(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ValidationFailed), EndpointHelpers.ToErrorDictionary(validation.Errors, localizer)));
        }

        var data = await repository.CreateAsync(user.CompanyId(), body, cancellationToken);
        return Results.Created($"/api/v1/quotes/{data.Id}", responseFactory.Success(httpContext, localizer.Get(MessageKeys.QuoteSaved), data));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        QuoteUpsertDto body,
        ClaimsPrincipal user,
        IValidator<QuoteUpsertDto> validator,
        IQuoteRepository repository,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(body, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.BadRequest(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ValidationFailed), EndpointHelpers.ToErrorDictionary(validation.Errors, localizer)));
        }

        var data = await repository.UpdateAsync(user.CompanyId(), id, body, cancellationToken);
        return data is null
            ? Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.QuoteNotFound)))
            : Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.QuoteSaved), data));
    }

    private static async Task<IResult> UpdateStatusAsync(
        Guid id,
        string status,
        ClaimsPrincipal user,
        IQuoteRepository repository,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var data = await repository.UpdateStatusAsync(user.CompanyId(), id, status, DateTime.UtcNow, convertedInvoiceDraftId: null, cancellationToken);
        return data is null
            ? Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.QuoteNotFound)))
            : Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.QuoteStatusUpdated), data));
    }

    private static async Task<IResult> ConvertToInvoiceAsync(
        Guid id,
        ClaimsPrincipal user,
        IQuoteRepository quoteRepository,
        IInvoiceDraftWorkflow invoiceDraftWorkflow,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var quote = await quoteRepository.GetAsync(user.CompanyId(), id, cancellationToken);
        if (quote is null)
        {
            return Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.QuoteNotFound)));
        }

        var invoiceDraftId = await invoiceDraftWorkflow.SaveDraftAsync(
            user.CompanyId(),
            quote.Document,
            quote.CustomerId,
            quote.ProductIds,
            cancellationToken);

        var updated = await quoteRepository.UpdateStatusAsync(
            user.CompanyId(),
            id,
            QuoteStatus.Converted,
            DateTime.UtcNow,
            invoiceDraftId,
            cancellationToken);

        return Results.Ok(responseFactory.Success(
            httpContext,
            localizer.Get(MessageKeys.QuoteConverted),
            new { invoiceDraftId, quote = updated }));
    }
}
