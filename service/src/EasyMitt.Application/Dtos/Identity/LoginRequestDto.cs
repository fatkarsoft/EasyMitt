namespace EasyMitt.Application.Dtos.Identity;

public sealed class LoginRequestDto
{
    public string Email { get; init; } = "";

    public string Password { get; init; } = "";
}
