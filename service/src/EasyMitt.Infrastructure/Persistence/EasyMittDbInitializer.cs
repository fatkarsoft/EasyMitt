using EasyMitt.Domain.Germany;
using EasyMitt.Infrastructure.Identity;
using EasyMitt.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EasyMitt.Infrastructure.Persistence;

public sealed class EasyMittDbInitializer(
    EasyMittDbContext db,
    IOptions<ConfiguredIdentityOptions> identityOptions)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        foreach (var configured in identityOptions.Value.Users)
        {
            var company = await db.Companies.FirstOrDefaultAsync(x => x.Id == configured.CompanyId, cancellationToken);
            if (company is null)
            {
                company = new CompanyEntity
                {
                    Id = configured.CompanyId,
                    Name = configured.CompanyName,
                    CountryCode = GermanCountryPolicy.CountryCode,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                };
                db.Companies.Add(company);
            }

            var email = configured.Email.Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(x => x.Id == configured.UserId, cancellationToken);
            if (user is null)
            {
                db.Users.Add(new UserEntity
                {
                    Id = configured.UserId,
                    CompanyId = configured.CompanyId,
                    Email = email,
                    PasswordHash = PasswordHashing.Hash(configured.Password),
                    DisplayName = configured.DisplayName,
                    Role = configured.Role,
                    Language = configured.Language,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                });
                continue;
            }

            user.CompanyId = configured.CompanyId;
            user.Email = email;
            user.DisplayName = configured.DisplayName;
            user.Role = configured.Role;
            user.Language = configured.Language;
            user.IsActive = true;
            user.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
