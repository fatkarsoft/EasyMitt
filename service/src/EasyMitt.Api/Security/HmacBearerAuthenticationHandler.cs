using System.Text.Encodings.Web;
using System.Text.Json;
using EasyMitt.Api.Responses;
using EasyMitt.Application.Abstractions.Identity;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Localization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EasyMitt.Api.Security;

public sealed class HmacBearerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IAuthTokenService tokenService,
    IAppLocalizer localizer,
    ApiResponseFactory responseFactory)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "EasyMittBearer";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        const string prefix = "Bearer ";
        if (!authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail(localizer.Get(MessageKeys.AuthenticationRequired)));
        }

        var token = authorization[prefix.Length..].Trim();
        var principal = tokenService.ValidateToken(token);
        if (principal is null)
        {
            return Task.FromResult(AuthenticateResult.Fail(localizer.Get(MessageKeys.AuthenticationRequired)));
        }

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        WriteErrorAsync(StatusCodes.Status401Unauthorized, localizer.Get(MessageKeys.AuthenticationRequired));

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        WriteErrorAsync(StatusCodes.Status403Forbidden, localizer.Get(MessageKeys.AuthorizationForbidden));

    private async Task WriteErrorAsync(int statusCode, string message)
    {
        Response.StatusCode = statusCode;
        Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(
            Response.Body,
            responseFactory.Failure(Context, message),
            cancellationToken: Context.RequestAborted);
    }
}
