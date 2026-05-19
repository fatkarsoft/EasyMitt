using System.Text.Json;
using EasyMitt.Api.Responses;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Localization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EasyMitt.Api.Features;

public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task WriteAsync(HttpContext httpContext, HealthReport report)
    {
        var responseFactory = httpContext.RequestServices.GetRequiredService<ApiResponseFactory>();
        var localizer = httpContext.RequestServices.GetRequiredService<IAppLocalizer>();
        var isReady = report.Status != HealthStatus.Unhealthy;

        var data = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = e.Value.Duration.TotalMilliseconds,
                data = e.Value.Data,
            }),
        };

        ApiResponse<object> envelope = isReady
            ? responseFactory.Success<object>(httpContext, localizer.Get(MessageKeys.SystemReady), data)
            : new ApiResponse<object>
            {
                Success = false,
                Message = localizer.Get(MessageKeys.SystemNotReady),
                Data = data,
                Errors = null,
                TraceId = httpContext.TraceIdentifier,
                Language = httpContext.RequestServices.GetRequiredService<ICurrentLanguage>().Language,
            };

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = isReady ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(envelope, JsonOptions));
    }
}
