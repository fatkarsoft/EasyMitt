namespace EasyMitt.Application.Dtos.Identity;

public sealed class LoginResponseDto
{
    public string AccessToken { get; init; } = "";

    public DateTimeOffset ExpiresAtUtc { get; init; }

    public AuthenticatedUserDto User { get; init; } = new();
}
