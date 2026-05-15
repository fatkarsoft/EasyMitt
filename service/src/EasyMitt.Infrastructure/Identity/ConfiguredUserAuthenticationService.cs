using EasyMitt.Application.Abstractions.Identity;
using EasyMitt.Application.Dtos.Identity;
using EasyMitt.Domain.Identity;
using EasyMitt.Domain.Localization;
using EasyMitt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EasyMitt.Infrastructure.Identity;

public sealed class ConfiguredUserAuthenticationService(
    EasyMittDbContext db) : IUserAuthenticationService
{
    public async Task<AuthenticatedUserDto?> AuthenticateAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var user = await db.Users
            .AsNoTracking()
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Email == email.ToLower(), cancellationToken);

        if (user is null ||
            user.Company is null ||
            !user.IsActive ||
            !EasyMittRoles.All.Contains(user.Role) ||
            !PasswordHashing.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        return new AuthenticatedUserDto
        {
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            CompanyId = user.CompanyId,
            CompanyName = user.Company.Name,
            Role = user.Role,
            Language = SupportedLanguages.NormalizeOrDefault(user.Language),
        };
    }
}
