namespace EasyMitt.Api.Responses;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }

    public string Message { get; init; } = "";

    public T? Data { get; init; }

    public IReadOnlyDictionary<string, IReadOnlyList<ApiError>>? Errors { get; init; }

    public string TraceId { get; init; } = "";

    public string Language { get; init; } = "en";
}

public sealed class ApiError
{
    public string Code { get; init; } = "";

    public string Message { get; init; } = "";
}
