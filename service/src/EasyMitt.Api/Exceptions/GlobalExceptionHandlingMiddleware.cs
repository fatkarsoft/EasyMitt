using EasyMitt.Api.Responses;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Localization;
using FluentValidation;

namespace EasyMitt.Api.Exceptions;

public sealed class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlingMiddleware> logger,
    IWebHostEnvironment environment)
{
    public async Task InvokeAsync(
        HttpContext context,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request was aborted by the client. TraceId: {TraceId}", context.TraceIdentifier);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, responseFactory, localizer);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        ApiResponseFactory responseFactory,
        IAppLocalizer localizer)
    {
        if (context.Response.HasStarted)
        {
            logger.LogError(
                exception,
                "Unhandled exception after response started. TraceId: {TraceId}",
                context.TraceIdentifier);
            throw exception;
        }

        var descriptor = ToDescriptor(exception, localizer);
        logger.LogError(
            exception,
            "Unhandled request exception. StatusCode: {StatusCode}. ErrorCode: {ErrorCode}. TraceId: {TraceId}",
            descriptor.StatusCode,
            descriptor.Code,
            context.TraceIdentifier);

        context.Response.Clear();
        context.Response.StatusCode = descriptor.StatusCode;
        context.Response.ContentType = "application/json";

        var errors = new Dictionary<string, IReadOnlyList<ApiError>>
        {
            ["exception"] =
            [
                new ApiError
                {
                    Code = descriptor.Code,
                    Message = environment.IsDevelopment()
                        ? $"{descriptor.Message} ({exception.GetType().Name}: {exception.Message})"
                        : descriptor.Message,
                },
            ],
        };

        await context.Response.WriteAsJsonAsync(
            responseFactory.Failure(context, descriptor.Message, errors),
            context.RequestAborted);
    }

    private static ExceptionDescriptor ToDescriptor(Exception exception, IAppLocalizer localizer) =>
        exception switch
        {
            ValidationException => new(
                StatusCodes.Status400BadRequest,
                MessageKeys.ValidationFailed,
                localizer.Get(MessageKeys.ValidationFailed)),
            ArgumentException or InvalidOperationException => new(
                StatusCodes.Status400BadRequest,
                MessageKeys.ErrorBadRequest,
                localizer.Get(MessageKeys.ErrorBadRequest)),
            KeyNotFoundException => new(
                StatusCodes.Status404NotFound,
                MessageKeys.ErrorNotFound,
                localizer.Get(MessageKeys.ErrorNotFound)),
            UnauthorizedAccessException => new(
                StatusCodes.Status403Forbidden,
                MessageKeys.AuthorizationForbidden,
                localizer.Get(MessageKeys.AuthorizationForbidden)),
            _ => new(
                StatusCodes.Status500InternalServerError,
                MessageKeys.ErrorUnexpected,
                localizer.Get(MessageKeys.ErrorUnexpected)),
        };

    private sealed record ExceptionDescriptor(int StatusCode, string Code, string Message);
}
