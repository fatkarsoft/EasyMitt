using System.Text;
using System.Text.Json;
using System.Security.Claims;
using EasyMitt.Api.Responses;
using EasyMitt.Api.Security;
using EasyMitt.Application.Abstractions.Communication;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Abstractions.Transformation;
using EasyMitt.Application.Dtos.Communication;
using EasyMitt.Application.Dtos.Billing;
using EasyMitt.Application.Dtos.En16931;
using EasyMitt.Application.Dtos.Ingestion;
using EasyMitt.Application.Exceptions;
using EasyMitt.Application.Localization;
using EasyMitt.Application.Services.Billing;
using EasyMitt.Application.Services.Transformation;
using EasyMitt.Domain.Billing;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace EasyMitt.Api.Features;

public static class InvoiceEndpoints
{
    public static void MapInvoiceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/invoices").WithTags("Invoices");

        group.MapPost("/validate", ValidateAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/drafts", SaveDraftAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapGet("/drafts", ListDraftsAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapGet("/drafts/{id:guid}", GetDraftAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapPost("/drafts/{id:guid}/issue", (Guid id, ClaimsPrincipal user, IInvoiceDraftRepository repository, IAppLocalizer localizer, ApiResponseFactory responseFactory, HttpContext httpContext, CancellationToken cancellationToken) =>
            UpdateDraftStatusAsync(id, InvoiceLifecycleStatus.Issued, user, repository, localizer, responseFactory, httpContext, cancellationToken)).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/drafts/{id:guid}/send", (Guid id, ClaimsPrincipal user, IInvoiceDraftRepository repository, IAppLocalizer localizer, ApiResponseFactory responseFactory, HttpContext httpContext, CancellationToken cancellationToken) =>
            UpdateDraftStatusAsync(id, InvoiceLifecycleStatus.Sent, user, repository, localizer, responseFactory, httpContext, cancellationToken)).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/drafts/{id:guid}/pay", (Guid id, ClaimsPrincipal user, IInvoiceDraftRepository repository, IAppLocalizer localizer, ApiResponseFactory responseFactory, HttpContext httpContext, CancellationToken cancellationToken) =>
            UpdateDraftStatusAsync(id, InvoiceLifecycleStatus.Paid, user, repository, localizer, responseFactory, httpContext, cancellationToken)).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/drafts/{id:guid}/overdue", (Guid id, ClaimsPrincipal user, IInvoiceDraftRepository repository, IAppLocalizer localizer, ApiResponseFactory responseFactory, HttpContext httpContext, CancellationToken cancellationToken) =>
            UpdateDraftStatusAsync(id, InvoiceLifecycleStatus.Overdue, user, repository, localizer, responseFactory, httpContext, cancellationToken)).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/drafts/{id:guid}/cancel", (Guid id, ClaimsPrincipal user, IInvoiceDraftRepository repository, IAppLocalizer localizer, ApiResponseFactory responseFactory, HttpContext httpContext, CancellationToken cancellationToken) =>
            UpdateDraftStatusAsync(id, InvoiceLifecycleStatus.Cancelled, user, repository, localizer, responseFactory, httpContext, cancellationToken)).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/ingest/raw", IngestRawAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/ingest/scan", IngestScanAsync)
            .DisableAntiforgery()
            .RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/export/xrechnung", ExportXRechnungAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/export/zugferd-pdf", ExportZugferdPdfAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/export/zugferd-pdf/with-layout", ExportZugferdPdfWithLayoutAsync)
            .DisableAntiforgery()
            .RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPost("/peppol/submit", PeppolSubmitAsync).RequireAuthorization(AuthorizationPolicies.InvoiceDispatch);
    }

    private static async Task<IResult> ValidateAsync(
        InvoiceDocumentDto body,
        IInvoiceDraftWorkflow workflow,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await workflow.ValidateAsync(body, cancellationToken);
        return ToValidationResult(result, localizer, responseFactory, httpContext);
    }

    private static async Task<IResult> SaveDraftAsync(
        InvoiceDraftSaveRequestDto body,
        ClaimsPrincipal user,
        IInvoiceDraftWorkflow workflow,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var id = await workflow.SaveDraftAsync(
                user.CompanyId(),
                body.Document,
                body.CustomerId,
                body.ProductIds,
                cancellationToken);
            return Results.Created(
                $"/api/v1/invoices/drafts/{id}",
                responseFactory.Success(httpContext, localizer.Get(MessageKeys.InvoiceDraftCreated), new { id }));
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(responseFactory.Failure(
                httpContext,
                localizer.Get(MessageKeys.ValidationFailed),
                ToErrorDictionary(ex.Errors, localizer)));
        }
    }

    private static async Task<IResult> GetDraftAsync(
        Guid id,
        ClaimsPrincipal user,
        IInvoiceDraftRepository repository,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var record = await repository.GetAsync(user.CompanyId(), id, cancellationToken);
        if (record is null)
        {
            return Results.NotFound(responseFactory.Failure(
                httpContext,
                localizer.Get(MessageKeys.InvoiceDraftNotFound)));
        }

        return Results.Ok(responseFactory.Success(
            httpContext,
            localizer.Get(MessageKeys.InvoiceDraftFound),
            ToDraftResponse(record)));
    }

    private static async Task<IResult> ListDraftsAsync(
        string? q,
        string? status,
        ClaimsPrincipal user,
        IInvoiceDraftRepository repository,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var records = await repository.ListAsync(user.CompanyId(), q, status, cancellationToken);
        return Results.Ok(responseFactory.Success(
            httpContext,
            localizer.Get(MessageKeys.InvoiceDraftFound),
            records.Select(ToDraftResponse).ToArray()));
    }

    private static async Task<IResult> UpdateDraftStatusAsync(
        Guid id,
        string nextStatus,
        ClaimsPrincipal user,
        IInvoiceDraftRepository repository,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var record = await repository.UpdateStatusAsync(user.CompanyId(), id, nextStatus, DateTime.UtcNow, cancellationToken);
            if (record is null)
            {
                return Results.NotFound(responseFactory.Failure(
                    httpContext,
                    localizer.Get(MessageKeys.InvoiceDraftNotFound)));
            }

            return Results.Ok(responseFactory.Success(
                httpContext,
                localizer.Get(MessageKeys.InvoiceStatusUpdated),
                ToDraftResponse(record)));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("invoice_lifecycle_transition_not_allowed", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(responseFactory.Failure(
                httpContext,
                localizer.Get(MessageKeys.InvoiceStatusInvalidTransition)));
        }
    }

    private static async Task<IResult> IngestRawAsync(
        RawInvoiceImportDto body,
        IRawInvoiceImportMapper mapper,
        IInvoiceDraftWorkflow workflow,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var document = mapper.MapFromRaw(body);
        var validation = await workflow.ValidateAsync(document, cancellationToken);
        var data = new
        {
            document,
            validation = new
            {
                valid = validation.IsValid,
                errors = validation.IsValid ? null : ToErrorDictionary(validation.Errors, localizer),
            },
        };

        return Results.Ok(responseFactory.Success(
            httpContext,
            localizer.Get(MessageKeys.InvoiceRawIngested),
            data));
    }

    private static async Task<IResult> IngestScanAsync(
        IFormFile? file,
        IScannedInvoiceImportAnalyzer analyzer,
        IRawInvoiceImportMapper mapper,
        IInvoiceDraftWorkflow workflow,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        IWebHostEnvironment environment,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(responseFactory.Failure(
                httpContext,
                localizer.Get(MessageKeys.InvoiceScanFileRequired)));
        }

        RawInvoiceImportDto raw;
        try
        {
            await using var stream = file.OpenReadStream();
            raw = await analyzer.AnalyzeAsync(stream, file.FileName, file.ContentType, cancellationToken);
        }
        catch (NotSupportedException)
        {
            return Results.BadRequest(responseFactory.Failure(
                httpContext,
                localizer.Get(MessageKeys.InvoiceScanUnsupportedFile)));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("scan_not_configured", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(responseFactory.Failure(
                httpContext,
                localizer.Get(MessageKeys.InvoiceScanNotConfigured)));
        }
        catch (InvoiceScanAnalysisException ex)
        {
            loggerFactory.CreateLogger("EasyMitt.InvoiceScan").LogWarning(
                ex,
                "Invoice scan analysis failed. Reason: {Reason}. Detail: {Detail}",
                ex.Reason,
                ex.Detail);

            var errors = environment.IsDevelopment()
                ? new Dictionary<string, IReadOnlyList<ApiError>>
                {
                    ["scan"] = new[]
                    {
                        new ApiError
                        {
                            Code = ex.Reason,
                            Message = ex.Detail ?? ex.Reason,
                        },
                    },
                }
                : null;

            return Results.BadRequest(responseFactory.Failure(
                httpContext,
                localizer.Get(MessageKeys.InvoiceScanFailed),
                errors));
        }
        catch (InvalidOperationException ex)
        {
            loggerFactory.CreateLogger("EasyMitt.InvoiceScan").LogWarning(
                ex,
                "Invoice scan request failed before analysis.");

            return Results.BadRequest(responseFactory.Failure(
                httpContext,
                localizer.Get(MessageKeys.InvoiceScanFailed)));
        }

        var document = mapper.MapFromRaw(raw);
        var validation = await workflow.ValidateAsync(document, cancellationToken);
        var data = new
        {
            raw,
            document,
            validation = new
            {
                valid = validation.IsValid,
                errors = validation.IsValid ? null : ToErrorDictionary(validation.Errors, localizer),
            },
        };

        return Results.Ok(responseFactory.Success(
            httpContext,
            localizer.Get(MessageKeys.InvoiceScanIngested),
            data));
    }

    private static async Task<IResult> ExportXRechnungAsync(
        InvoiceDocumentDto body,
        IValidator<InvoiceDocumentDto> validator,
        IElectronicInvoiceGenerator generator,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(body, cancellationToken);
        if (!validation.IsValid)
        {
            return ToValidationFailureResult(validation, localizer, responseFactory, httpContext);
        }

        var xml = await generator.GenerateXRechnungXmlAsync(body, cancellationToken);
        var fileName = $"xrechnung-{SanitizeFileToken(body.Core.InvoiceNumber)}.xml";
        return Results.File(xml, "application/xml", fileName);
    }

    private static async Task<IResult> ExportZugferdPdfAsync(
        InvoiceDocumentDto body,
        IValidator<InvoiceDocumentDto> validator,
        IElectronicInvoiceGenerator generator,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(body, cancellationToken);
        if (!validation.IsValid)
        {
            return ToValidationFailureResult(validation, localizer, responseFactory, httpContext);
        }

        var pdf = await generator.GenerateZugferdPdfAsync(body, visualPdf: null, cancellationToken);
        var fileName = $"zugferd-{SanitizeFileToken(body.Core.InvoiceNumber)}.pdf";
        return Results.File(pdf, "application/pdf", fileName);
    }

    private static async Task<IResult> ExportZugferdPdfWithLayoutAsync(
        [FromForm] string document,
        IFormFile? layout,
        IValidator<InvoiceDocumentDto> validator,
        IElectronicInvoiceGenerator generator,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        InvoiceDocumentDto? body;
        try
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            body = JsonSerializer.Deserialize<InvoiceDocumentDto>(document, jsonOptions);
        }
        catch (JsonException)
        {
            return Results.BadRequest(responseFactory.Failure(
                httpContext,
                localizer.Get(MessageKeys.ValidationInvalidJson)));
        }

        if (body is null)
        {
            return Results.BadRequest(responseFactory.Failure(
                httpContext,
                localizer.Get(MessageKeys.ValidationDocumentRequired)));
        }

        var validation = await validator.ValidateAsync(body, cancellationToken);
        if (!validation.IsValid)
        {
            return ToValidationFailureResult(validation, localizer, responseFactory, httpContext);
        }

        byte[] pdf;
        if (layout is null)
        {
            pdf = await generator.GenerateZugferdPdfAsync(body, visualPdf: null, cancellationToken);
        }
        else
        {
            await using var layoutStream = layout.OpenReadStream();
            pdf = await generator.GenerateZugferdPdfAsync(body, layoutStream, cancellationToken);
        }

        var fileName = $"zugferd-{SanitizeFileToken(body.Core.InvoiceNumber)}.pdf";
        return Results.File(pdf, "application/pdf", fileName);
    }

    private static async Task<IResult> PeppolSubmitAsync(
        PeppolSubmitRequestDto body,
        IValidator<InvoiceDocumentDto> validator,
        IElectronicInvoiceGenerator generator,
        IInvoiceDispatch dispatch,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(body.Document, cancellationToken);
        if (!validation.IsValid)
        {
            return ToValidationFailureResult(validation, localizer, responseFactory, httpContext);
        }

        var xml = await generator.GenerateXRechnungXmlAsync(body.Document, cancellationToken);
        var receipt = await dispatch.SubmitAsync(
            new InvoiceDispatchRequest(body.Document, xml, "application/xml", body.RecipientEndpointId),
            cancellationToken);

        return Results.Ok(responseFactory.Success(
            httpContext,
            localizer.Get(MessageKeys.InvoicePeppolSubmitted),
            new
            {
                receipt.DispatchId,
                receipt.Status,
                receipt.Metadata,
                xrechnungBytes = xml.Length,
            }));
    }

    private static IResult ToValidationResult(
        ValidationResult result,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext) =>
        result.IsValid
            ? Results.Ok(responseFactory.Success(
                httpContext,
                localizer.Get(MessageKeys.InvoiceValidationSucceeded),
                new { valid = true }))
            : ToValidationFailureResult(result, localizer, responseFactory, httpContext);

    private static object ToDraftResponse(InvoiceDraftRecord record)
    {
        var document = JsonSerializer.Deserialize<JsonElement>(record.PayloadJson);
        return new
        {
            record.Id,
            record.CustomerId,
            record.ProductIds,
            record.CanonicalSha256Hex,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.Status,
            record.IssuedAtUtc,
            record.SentAtUtc,
            record.PaidAtUtc,
            record.CancelledAtUtc,
            record.IsImmutableSnapshot,
            record.ArchiveObjectKey,
            document,
        };
    }

    private static IResult ToValidationFailureResult(
        ValidationResult validation,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext) =>
        Results.BadRequest(responseFactory.Failure(
            httpContext,
            localizer.Get(MessageKeys.ValidationFailed),
            ToErrorDictionary(validation.Errors, localizer)));

    private static Dictionary<string, IReadOnlyList<ApiError>> ToErrorDictionary(
        IEnumerable<FluentValidation.Results.ValidationFailure> failures,
        IAppLocalizer localizer) =>
        failures
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ApiError>)g.Select(e => new ApiError
                {
                    Code = e.ErrorCode,
                    Message = localizer.GetValidationMessage(e),
                }).ToArray());

    private static string SanitizeFileToken(string invoiceNumber)
    {
        var sb = new StringBuilder(invoiceNumber.Length);
        foreach (var c in invoiceNumber)
        {
            if (char.IsLetterOrDigit(c) || c is '-' or '_')
            {
                sb.Append(c);
            }
        }

        return sb.Length > 0 ? sb.ToString() : "invoice";
    }
}
