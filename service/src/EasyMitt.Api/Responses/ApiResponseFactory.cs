using EasyMitt.Application.Abstractions.Localization;

namespace EasyMitt.Api.Responses;

public sealed class ApiResponseFactory(ICurrentLanguage currentLanguage)
{
    public ApiResponse<T> Success<T>(HttpContext httpContext, string message, T data) =>
        Success(httpContext, message, data, currentLanguage.Language);

    public ApiResponse<T> Success<T>(HttpContext httpContext, string message, T data, string language) =>
        new()
        {
            Success = true,
            Message = message,
            Data = data,
            Errors = null,
            TraceId = httpContext.TraceIdentifier,
            Language = language,
        };

    public ApiResponse<object> Success(HttpContext httpContext, string message) =>
        new()
        {
            Success = true,
            Message = message,
            Data = null,
            Errors = null,
            TraceId = httpContext.TraceIdentifier,
            Language = currentLanguage.Language,
        };

    public ApiResponse<object> Failure(
        HttpContext httpContext,
        string message,
        IReadOnlyDictionary<string, IReadOnlyList<ApiError>>? errors = null) =>
        new()
        {
            Success = false,
            Message = message,
            Data = null,
            Errors = errors,
            TraceId = httpContext.TraceIdentifier,
            Language = currentLanguage.Language,
        };
}
