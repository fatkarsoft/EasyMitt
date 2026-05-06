namespace EasyMitt.Infrastructure.Identity;

public sealed class ConfiguredIdentityOptions
{
    public string SigningKey { get; init; } = "";

    public int TokenLifetimeMinutes { get; init; } = 60;

    public IReadOnlyList<ConfiguredUserOptions> Users { get; init; } = Array.Empty<ConfiguredUserOptions>();
}

public sealed class ConfiguredUserOptions
{
    public Guid UserId { get; init; }

    public string Email { get; init; } = "";

    public string Password { get; init; } = "";

    public string DisplayName { get; init; } = "";

    public Guid CompanyId { get; init; }

    public string CompanyName { get; init; } = "";

    public string Role { get; init; } = "";

    public string Language { get; init; } = "en";
}
