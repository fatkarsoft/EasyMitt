using System.Security.Claims;
using EasyMitt.Api.Responses;
using EasyMitt.Api.Security;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Inventory;
using EasyMitt.Application.Localization;
using FluentValidation;

namespace EasyMitt.Api.Features;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/inventory").WithTags("Inventory");
        group.MapGet("/movements", ListAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapPost("/movements", CreateAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
    }

    private static async Task<IResult> ListAsync(Guid? productId, ClaimsPrincipal user, IInventoryRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var data = await repository.ListMovementsAsync(user.CompanyId(), productId, cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.InventoryMovementsFound), data));
    }

    private static async Task<IResult> CreateAsync(InventoryMovementCreateDto body, ClaimsPrincipal user, IValidator<InventoryMovementCreateDto> validator, IInventoryRepository repository, ApiResponseFactory responseFactory, IAppLocalizer localizer, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(body, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.BadRequest(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ValidationFailed), EndpointHelpers.ToErrorDictionary(validation.Errors, localizer)));
        }

        var data = await repository.CreateMovementAsync(user.CompanyId(), body, cancellationToken);
        return data is null
            ? Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.ProductNotFound)))
            : Results.Created($"/api/v1/inventory/movements/{data.Id}", responseFactory.Success(httpContext, localizer.Get(MessageKeys.InventoryMovementSaved), data));
    }
}
