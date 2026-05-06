using EasyMitt.Application.Abstractions.Identity;
using EasyMitt.Application.Abstractions.Archiving;
using EasyMitt.Application.Abstractions.Communication;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Abstractions.Transformation;
using EasyMitt.Infrastructure.Archiving;
using EasyMitt.Infrastructure.Communication;
using EasyMitt.Infrastructure.ElectronicInvoicing;
using EasyMitt.Infrastructure.Identity;
using EasyMitt.Infrastructure.Persistence;
using EasyMitt.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EasyMitt.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("ConnectionStrings:PostgreSQL tanımlı değil.");

        services.AddDbContext<EasyMittDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IInvoiceDraftRepository, InvoiceDraftRepository>();

        var configuredArchiveRoot = configuration["Archive:LocalRoot"];
        var archiveRoot = string.IsNullOrWhiteSpace(configuredArchiveRoot)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasyMitt", "archive")
            : configuredArchiveRoot!;

        services.AddSingleton<IImmutableArchiveStore>(_ => new LocalFileImmutableArchiveStore(archiveRoot));

        services.AddSingleton<IElectronicInvoiceGenerator, S2ElectronicInvoiceGenerator>();
        services.AddSingleton<IInvoiceDispatch, NoOpInvoiceDispatch>();
        services.AddSingleton(Options.Create(ReadIdentityOptions(configuration)));
        services.AddSingleton<IAuthTokenService, HmacAuthTokenService>();
        services.AddSingleton<IUserAuthenticationService, ConfiguredUserAuthenticationService>();

        return services;
    }

    private static ConfiguredIdentityOptions ReadIdentityOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection("Authentication");
        var users = section.GetSection("Users")
            .GetChildren()
            .Select(user => new ConfiguredUserOptions
            {
                UserId = Guid.Parse(user["UserId"] ?? throw new InvalidOperationException("Authentication:Users:UserId tanımlı değil.")),
                Email = user["Email"] ?? "",
                Password = user["Password"] ?? "",
                DisplayName = user["DisplayName"] ?? "",
                CompanyId = Guid.Parse(user["CompanyId"] ?? throw new InvalidOperationException("Authentication:Users:CompanyId tanımlı değil.")),
                CompanyName = user["CompanyName"] ?? "",
                Role = user["Role"] ?? "",
                Language = user["Language"] ?? "en",
            })
            .ToArray();

        return new ConfiguredIdentityOptions
        {
            SigningKey = section["SigningKey"] ?? "",
            TokenLifetimeMinutes = int.TryParse(section["TokenLifetimeMinutes"], out var minutes) ? minutes : 60,
            Users = users,
        };
    }
}
