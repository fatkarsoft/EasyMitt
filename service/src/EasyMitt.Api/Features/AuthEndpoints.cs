using System.Security.Claims;
using EasyMitt.Api.Responses;
using EasyMitt.Application.Abstractions.Identity;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Dtos.Identity;
using EasyMitt.Application.Localization;
using EasyMitt.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace EasyMitt.Api.Features;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Authentication");

        group.MapPost("/login", LoginAsync).AllowAnonymous();
        group.MapGet("/me", Me).RequireAuthorization();
    }

    private static async Task<IResult> LoginAsync(
        LoginRequestDto body,
        IUserAuthenticationService authenticationService,
        IAuthTokenService tokenService,
        IOptions<ConfiguredIdentityOptions> options,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var user = await authenticationService.AuthenticateAsync(body, cancellationToken);
        if (user is null)
        {
            return Results.Json(
                responseFactory.Failure(httpContext, localizer.Get(MessageKeys.AuthenticationInvalidCredentials)),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(Math.Max(1, options.Value.TokenLifetimeMinutes));
        var response = new LoginResponseDto
        {
            AccessToken = tokenService.CreateToken(user, expiresAt),
            ExpiresAtUtc = expiresAt,
            User = user,
        };

        return Results.Ok(responseFactory.Success(
            httpContext,
            localizer.Get(MessageKeys.Authenticated, user.Language),
            response,
            user.Language));
    }

    private static IResult Me(
        ClaimsPrincipal user,
        IAppLocalizer localizer,
        ApiResponseFactory responseFactory,
        HttpContext httpContext)
    {
        return Results.Ok(responseFactory.Success(
            httpContext,
            localizer.Get(MessageKeys.Authenticated),
            new
            {
                userId = user.FindFirstValue(ClaimTypes.NameIdentifier),
                email = user.FindFirstValue(ClaimTypes.Email),
                displayName = user.FindFirstValue(ClaimTypes.Name),
                companyId = user.FindFirstValue("company_id"),
                companyName = user.FindFirstValue("company_name"),
                role = user.FindFirstValue(ClaimTypes.Role),
                language = user.FindFirstValue("language"),
            }));
    }
}
