using System.Security.Claims;
using System.Text.Json;
using EasyMitt.Api.Responses;
using EasyMitt.Api.Security;
using EasyMitt.Application.Abstractions.Ai;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Ai;
using EasyMitt.Application.Localization;

namespace EasyMitt.Api.Features;

public static class AiEndpoints
{
    public sealed class DatevSuggestRequest
    {
        public string DocumentType { get; init; } = "Expense";
        public Guid DocumentId { get; init; }
    }

    public sealed class CategorySuggestRequest
    {
        public string VendorName { get; init; } = "";
        public string? LineDescriptions { get; init; }
        public decimal? TotalAmount { get; init; }
        public string? CurrencyCode { get; init; }
    }

    public sealed class DecideRequest
    {
        public AiSuggestionCreateRequest? Snapshot { get; init; }
    }

    public static void MapAiEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/ai").WithTags("AI");
        group.MapGet("/suggestions", ListAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapGet("/suggestions/{id:guid}", GetAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapPost("/suggestions/{id:guid}/accept", AcceptAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/suggestions/{id:guid}/reject", RejectAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/suggestions/{id:guid}/retry", RetryAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/suggestions/log", LogAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/category-suggest", CategorySuggestAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapPost("/datev-suggest", DatevSuggestAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapGet("/payment-suggest/{bankTransactionId:guid}", PaymentSuggestAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapGet("/invoice-field-suggest/{invoiceDraftId:guid}", InvoiceFieldSuggestAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
    }

    private static async Task<IResult> ListAsync(
        string? suggestionType,
        string? status,
        string? targetType,
        Guid? targetId,
        int? take,
        ClaimsPrincipal user,
        IAiSuggestionRepository repository,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var data = await repository.SearchAsync(user.CompanyId(), suggestionType, status, targetType, targetId, take ?? 100, cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.AiSuggestionsFound), data));
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ClaimsPrincipal user,
        IAiSuggestionRepository repository,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var data = await repository.GetAsync(user.CompanyId(), id, cancellationToken);
        return data is null
            ? Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.AiSuggestionNotFound)))
            : Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.AiSuggestionsFound), data));
    }

    private static async Task<IResult> AcceptAsync(
        Guid id,
        ClaimsPrincipal user,
        IAiSuggestionRepository repository,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var data = await repository.DecideAsync(user.CompanyId(), id, AiSuggestionStatuses.Accepted, UserEmail(user), cancellationToken);
        return data is null
            ? Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.AiSuggestionNotFound)))
            : Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.AiSuggestionAccepted), data));
    }

    private static async Task<IResult> RejectAsync(
        Guid id,
        ClaimsPrincipal user,
        IAiSuggestionRepository repository,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var data = await repository.DecideAsync(user.CompanyId(), id, AiSuggestionStatuses.Rejected, UserEmail(user), cancellationToken);
        return data is null
            ? Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.AiSuggestionNotFound)))
            : Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.AiSuggestionRejected), data));
    }

    private static async Task<IResult> RetryAsync(
        Guid id,
        DecideRequest? body,
        ClaimsPrincipal user,
        IAiSuggestionRepository repository,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var existing = await repository.GetAsync(user.CompanyId(), id, cancellationToken);
        if (existing is null)
            return Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.AiSuggestionNotFound)));

        var snapshot = body?.Snapshot ?? new AiSuggestionCreateRequest
        {
            SuggestionType = existing.SuggestionType,
            TargetType = existing.TargetType,
            TargetId = existing.TargetId,
            Payload = existing.Payload,
        };
        var fresh = await repository.RecordAsync(user.CompanyId(), snapshot, AiSuggestionStatuses.Pending, null, cancellationToken);
        await repository.SupersedeAsync(user.CompanyId(), id, fresh.Id, UserEmail(user), cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.AiSuggestionRetried), fresh));
    }

    private static async Task<IResult> LogAsync(
        AiSuggestionCreateRequest body,
        string? status,
        ClaimsPrincipal user,
        IAiSuggestionRepository repository,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var resolvedStatus = status switch
        {
            AiSuggestionStatuses.Accepted => AiSuggestionStatuses.Accepted,
            AiSuggestionStatuses.Rejected => AiSuggestionStatuses.Rejected,
            AiSuggestionStatuses.Superseded => AiSuggestionStatuses.Superseded,
            _ => AiSuggestionStatuses.Pending,
        };
        var data = await repository.RecordAsync(user.CompanyId(), body, resolvedStatus, resolvedStatus == AiSuggestionStatuses.Pending ? null : UserEmail(user), cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.AiSuggestionsFound), data));
    }

    private static IResult CategorySuggestAsync(
        CategorySuggestRequest body,
        IExpenseCategorySuggester suggester,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext)
    {
        var data = suggester.SuggestFromFields(body.VendorName, body.LineDescriptions, body.TotalAmount, body.CurrencyCode);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.AiCategorySuggested), data));
    }

    private static async Task<IResult> DatevSuggestAsync(
        DatevSuggestRequest body,
        ClaimsPrincipal user,
        IDatevAccountSuggester suggester,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var data = await suggester.SuggestAsync(user.CompanyId(), body.DocumentType, body.DocumentId, cancellationToken);
        return data is null
            ? Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.AiDocumentNotFound)))
            : Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.AiDatevAccountSuggested), data));
    }

    private static async Task<IResult> PaymentSuggestAsync(
        Guid bankTransactionId,
        ClaimsPrincipal user,
        IPaymentMatchScorer scorer,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var data = await scorer.SuggestAsync(user.CompanyId(), bankTransactionId, cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.AiPaymentMatchSuggested), data));
    }

    private static async Task<IResult> InvoiceFieldSuggestAsync(
        Guid invoiceDraftId,
        ClaimsPrincipal user,
        IComplianceRepository compliance,
        IMissingFieldSuggester suggester,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dashboard = await compliance.GetDashboardAsync(user.CompanyId(), today, null, null, null, null, cancellationToken);
        var doc = dashboard.Documents.FirstOrDefault(x => x.InvoiceDraftId == invoiceDraftId);
        var risks = doc?.Risks ?? Array.Empty<string>();
        var data = await suggester.SuggestAsync(user.CompanyId(), invoiceDraftId, risks, cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.AiInvoiceFieldsSuggested), data));
    }

    private static string UserEmail(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email") ?? user.Identity?.Name ?? "";
}
