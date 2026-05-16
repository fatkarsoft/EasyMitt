using System.Security.Claims;
using System.Text.Json;
using EasyMitt.Api.Responses;
using EasyMitt.Api.Security;
using EasyMitt.Application.Abstractions.Email;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Abstractions.Transformation;
using EasyMitt.Application.Dtos.Email;
using EasyMitt.Application.Dtos.En16931;
using EasyMitt.Application.Localization;

namespace EasyMitt.Api.Features;

public static class EmailEndpoints
{
    public static void MapEmailEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/email").WithTags("Email");

        group.MapPost("/invoices/{id:guid}/send", SendInvoiceEmailAsync)
            .RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapGet("/invoices/{id:guid}/logs", GetInvoiceLogsAsync)
            .RequireAuthorization(AuthorizationPolicies.InvoiceRead);

        group.MapPost("/quotes/{id:guid}/send", SendQuoteEmailAsync)
            .RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapGet("/quotes/{id:guid}/logs", GetQuoteLogsAsync)
            .RequireAuthorization(AuthorizationPolicies.InvoiceRead);

        group.MapPost("/dunning/{id:guid}/send", SendDunningEmailAsync)
            .RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapGet("/logs", GetRecentLogsAsync)
            .RequireAuthorization(AuthorizationPolicies.InvoiceRead);
    }

    private static async Task<IResult> SendInvoiceEmailAsync(
        Guid id,
        SendInvoiceEmailRequestDto body,
        ClaimsPrincipal user,
        IInvoiceDraftRepository draftRepository,
        IElectronicInvoiceGenerator generator,
        IEmailService emailService,
        IEmailDeliveryLogRepository logRepository,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var draft = await draftRepository.GetAsync(user.CompanyId(), id, cancellationToken);
        if (draft is null)
            return Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.EmailDocumentNotFound)));

        InvoiceDocumentDto? document = null;
        byte[]? pdfBytes = null;
        string attachmentType = "none";

        try
        {
            document = JsonSerializer.Deserialize<InvoiceDocumentDto>(draft.PayloadJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { }

        if (document is not null)
        {
            try
            {
                pdfBytes = await generator.GenerateZugferdPdfAsync(document, null, cancellationToken);
                attachmentType = "zugferd-pdf";
            }
            catch { attachmentType = "none"; }
        }

        var invoiceNumber = document?.Core?.InvoiceNumber ?? id.ToString();
        var fileName = $"Rechnung-{SanitizeToken(invoiceNumber)}.pdf";

        var result = await emailService.SendAsync(new EmailMessage(
            body.ToEmail,
            body.Subject,
            body.Body,
            pdfBytes is not null ? fileName : null,
            pdfBytes,
            pdfBytes is not null ? "application/pdf" : null), cancellationToken);

        var status = result.Success ? "Sent" : "Failed";
        await logRepository.AddAsync(new EmailDeliveryLogEntry(
            user.CompanyId(), "Invoice", id, body.ToEmail, body.Subject,
            attachmentType, status, result.ErrorMessage,
            user.UserId().ToString(), user.FindFirstValue(ClaimTypes.Email) ?? ""), cancellationToken);

        return result.Success
            ? Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.EmailSent), new { invoiceId = id }))
            : Results.Ok(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.EmailFailed)));
    }

    private static async Task<IResult> SendQuoteEmailAsync(
        Guid id,
        SendQuoteEmailRequestDto body,
        ClaimsPrincipal user,
        IQuoteRepository quoteRepository,
        IEmailService emailService,
        IEmailDeliveryLogRepository logRepository,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var quote = await quoteRepository.GetAsync(user.CompanyId(), id, cancellationToken);
        if (quote is null)
            return Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.EmailDocumentNotFound)));

        var result = await emailService.SendAsync(new EmailMessage(
            body.ToEmail, body.Subject, body.Body), cancellationToken);

        var status = result.Success ? "Sent" : "Failed";
        await logRepository.AddAsync(new EmailDeliveryLogEntry(
            user.CompanyId(), "Quote", id, body.ToEmail, body.Subject,
            "none", status, result.ErrorMessage,
            user.UserId().ToString(), user.FindFirstValue(ClaimTypes.Email) ?? ""), cancellationToken);

        return result.Success
            ? Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.EmailSent), new { quoteId = id }))
            : Results.Ok(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.EmailFailed)));
    }

    private static async Task<IResult> SendDunningEmailAsync(
        Guid id,
        SendDunningEmailRequestDto body,
        ClaimsPrincipal user,
        IInvoiceDraftRepository draftRepository,
        IEmailService emailService,
        IEmailDeliveryLogRepository logRepository,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var draft = await draftRepository.GetAsync(user.CompanyId(), id, cancellationToken);
        if (draft is null)
            return Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.EmailDocumentNotFound)));

        var result = await emailService.SendAsync(new EmailMessage(
            body.ToEmail, body.Subject, body.Body), cancellationToken);

        var status = result.Success ? "Sent" : "Failed";
        await logRepository.AddAsync(new EmailDeliveryLogEntry(
            user.CompanyId(), "Dunning", id, body.ToEmail, body.Subject,
            "none", status, result.ErrorMessage,
            user.UserId().ToString(), user.FindFirstValue(ClaimTypes.Email) ?? ""), cancellationToken);

        return result.Success
            ? Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.EmailSent), new { invoiceId = id }))
            : Results.Ok(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.EmailFailed)));
    }

    private static async Task<IResult> GetInvoiceLogsAsync(
        Guid id,
        ClaimsPrincipal user,
        IEmailDeliveryLogRepository logRepository,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var logs = await logRepository.GetByDocumentAsync(user.CompanyId(), "Invoice", id, cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.EmailLogsFound), logs));
    }

    private static async Task<IResult> GetQuoteLogsAsync(
        Guid id,
        ClaimsPrincipal user,
        IEmailDeliveryLogRepository logRepository,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var logs = await logRepository.GetByDocumentAsync(user.CompanyId(), "Quote", id, cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.EmailLogsFound), logs));
    }

    private static async Task<IResult> GetRecentLogsAsync(
        ClaimsPrincipal user,
        IEmailDeliveryLogRepository logRepository,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var logs = await logRepository.GetRecentAsync(user.CompanyId(), 100, cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.EmailLogsFound), logs));
    }

    private static string SanitizeToken(string value)
    {
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '-' && chars[i] != '_')
                chars[i] = '_';
        }
        return new string(chars);
    }
}
