namespace EasyMitt.Application.Dtos.Identity;

public sealed class AuthenticatedUserDto
{
    public Guid UserId { get; init; }

    public string Email { get; init; } = "";

    public string DisplayName { get; init; } = "";

    public Guid CompanyId { get; init; }

    public string CompanyName { get; init; } = "";

    public string Role { get; init; } = "";

    public string Language { get; init; } = "en";
}
