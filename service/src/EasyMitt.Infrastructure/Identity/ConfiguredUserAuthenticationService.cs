using EasyMitt.Application.Abstractions.Identity;
using EasyMitt.Application.Dtos.Identity;
using EasyMitt.Domain.Identity;
using EasyMitt.Domain.Localization;
using Microsoft.Extensions.Options;

namespace EasyMitt.Infrastructure.Identity;

public sealed class ConfiguredUserAuthenticationService(
    IOptions<ConfiguredIdentityOptions> options) : IUserAuthenticationService
{
    public Task<AuthenticatedUserDto?> AuthenticateAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var user = options.Value.Users.FirstOrDefault(u =>
            string.Equals(u.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(u.Password, request.Password, StringComparison.Ordinal));

        if (user is null || !EasyMittRoles.All.Contains(user.Role))
        {
            return Task.FromResult<AuthenticatedUserDto?>(null);
        }

        return Task.FromResult<AuthenticatedUserDto?>(new AuthenticatedUserDto
        {
            UserId = user.UserId,
            Email = user.Email,
            DisplayName = user.DisplayName,
            CompanyId = user.CompanyId,
            CompanyName = user.CompanyName,
            Role = user.Role,
            Language = SupportedLanguages.NormalizeOrDefault(user.Language),
        });
    }
}
