using System.Security.Claims;
using EasyMitt.Api.Responses;
using EasyMitt.Api.Security;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Dtos.Customers;
using EasyMitt.Application.Localization;
using FluentValidation;

namespace EasyMitt.Api.Features;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/customers").WithTags("Customers");
        group.MapGet("/", SearchAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapGet("/{id:guid}", GetAsync).RequireAuthorization(AuthorizationPolicies.InvoiceRead);
        group.MapPost("/", CreateAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapPut("/{id:guid}", UpdateAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
        group.MapDelete("/{id:guid}", DeleteAsync).RequireAuthorization(AuthorizationPolicies.InvoiceWrite);
    }

    private static async Task<IResult> SearchAsync(
        string? q,
        bool? includeInactive,
        ClaimsPrincipal user,
        ICustomerRepository repository,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var data = await repository.SearchAsync(user.CompanyId(), q, includeInactive == true, cancellationToken);
        return Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.CustomersFound), data));
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ClaimsPrincipal user,
        ICustomerRepository repository,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var data = await repository.GetAsync(user.CompanyId(), id, cancellationToken);
        return data is null
            ? Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.CustomerNotFound)))
            : Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.CustomersFound), data));
    }

    private static async Task<IResult> CreateAsync(
        CustomerUpsertDto body,
        ClaimsPrincipal user,
        IValidator<CustomerUpsertDto> validator,
        ICustomerRepository repository,
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
        return Results.Created($"/api/v1/customers/{data.Id}", responseFactory.Success(httpContext, localizer.Get(MessageKeys.CustomerSaved), data));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        CustomerUpsertDto body,
        ClaimsPrincipal user,
        IValidator<CustomerUpsertDto> validator,
        ICustomerRepository repository,
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
            ? Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.CustomerNotFound)))
            : Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.CustomerSaved), data));
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ClaimsPrincipal user,
        ICustomerRepository repository,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteAsync(user.CompanyId(), id, cancellationToken);
        return deleted
            ? Results.Ok(responseFactory.Success(httpContext, localizer.Get(MessageKeys.CustomerDeleted), new { id }))
            : Results.NotFound(responseFactory.Failure(httpContext, localizer.Get(MessageKeys.CustomerNotFound)));
    }
}
