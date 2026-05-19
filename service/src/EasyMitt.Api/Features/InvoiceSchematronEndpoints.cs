using System.Security.Claims;
using System.Text.Json;
using EasyMitt.Api.Responses;
using EasyMitt.Api.Security;
using EasyMitt.Application.Abstractions.Compliance;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.En16931;
using EasyMitt.Application.Localization;

namespace EasyMitt.Api.Features;

public static class InvoiceSchematronEndpoints
{
    public static void MapInvoiceSchematronEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/invoices/{id:guid}/validate-schematron", ValidateAsync)
            .WithTags("Invoices")
            .RequireAuthorization(AuthorizationPolicies.InvoiceRead);
    }

    private static async Task<IResult> ValidateAsync(
        Guid id,
        ClaimsPrincipal user,
        IInvoiceDraftRepository repository,
        IInvoiceSchematronValidator validator,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var draft = await repository.GetAsync(user.CompanyId(), id, cancellationToken);
        if (draft is null)
            return Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.InvoiceDraftNotFound)));

        InvoiceDocumentDto? document;
        try
        {
            document = JsonSerializer.Deserialize<InvoiceDocumentDto>(draft.PayloadJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return Results.BadRequest(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ValidationInvalidJson)));
        }

        if (document is null)
            return Results.BadRequest(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ValidationDocumentRequired)));

        var result = validator.Validate(document);
        return Results.Ok(responseFactory.Success(
            httpContext,
            localizer.Get(result.IsValid ? MessageKeys.SchematronValid : MessageKeys.SchematronInvalid),
            result));
    }
}
