using EasyMitt.Application.Abstractions.Ai;
using EasyMitt.Application.Abstractions.Email;
using EasyMitt.Application.Abstractions.Identity;
using EasyMitt.Application.Abstractions.Portal;
using EasyMitt.Infrastructure.Ai;
using EasyMitt.Infrastructure.Portal;
using EasyMitt.Application.Abstractions.Archiving;
using EasyMitt.Application.Abstractions.Communication;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Abstractions.Transformation;
using EasyMitt.Infrastructure.Archiving;
using EasyMitt.Infrastructure.Communication;
using EasyMitt.Infrastructure.ElectronicInvoicing;
using EasyMitt.Infrastructure.Email;
using EasyMitt.Infrastructure.Identity;
using EasyMitt.Infrastructure.Ingestion;
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
            options
                .UseNpgsql(connectionString)
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<EasyMittDbInitializer>();
        services.AddScoped<IInvoiceDraftRepository, InvoiceDraftRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IDunningRepository, DunningRepository>();
        services.AddScoped<IReportingRepository, ReportingRepository>();
        services.AddScoped<IDatevSettingsRepository, DatevSettingsRepository>();
        services.AddScoped<IDatevExportLogRepository, DatevExportLogRepository>();
        services.AddScoped<IComplianceRepository, ComplianceRepository>();
        services.AddScoped<IEmailDeliveryLogRepository, EmailDeliveryLogRepository>();
        services.AddScoped<ICustomerPortalAccessRepository, CustomerPortalAccessRepository>();
        services.AddSingleton<IPortalTokenGenerator, PortalTokenGenerator>();
        services.AddScoped<IAiSuggestionRepository, AiSuggestionRepository>();
        services.AddSingleton<IExpenseCategorySuggester, ExpenseCategorySuggester>();
        services.AddScoped<IDatevAccountSuggester, DatevAccountSuggester>();
        services.AddScoped<IPaymentMatchScorer, PaymentMatchScorerService>();
        services.AddScoped<IMissingFieldSuggester, MissingFieldSuggester>();

        var emailOptions = ReadEmailOptions(configuration);
        services.AddSingleton(Options.Create(emailOptions));
        if (emailOptions.IsConfigured)
            services.AddSingleton<IEmailService, SmtpEmailService>();
        else
            services.AddSingleton<IEmailService, NoOpEmailService>();

        var configuredArchiveRoot = configuration["Archive:LocalRoot"];
        var archiveRoot = string.IsNullOrWhiteSpace(configuredArchiveRoot)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasyMitt", "archive")
            : configuredArchiveRoot!;

        services.AddSingleton<IImmutableArchiveStore>(_ => new LocalFileImmutableArchiveStore(archiveRoot));

        services.AddSingleton<IElectronicInvoiceGenerator, S2ElectronicInvoiceGenerator>();
        services.AddSingleton<IInvoiceDispatch, NoOpInvoiceDispatch>();
        var scanServiceOptions = ReadScanServiceOptions(configuration);
        services.AddSingleton(Options.Create(scanServiceOptions));
        services.AddSingleton<IScannedInvoiceImportAnalyzer>(sp =>
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(scanServiceOptions.BaseUrl),
                Timeout = TimeSpan.FromSeconds(Math.Max(scanServiceOptions.TimeoutSeconds, 1)),
            };

            return new ScanServiceInvoiceScanAnalyzer(
                httpClient,
                sp.GetRequiredService<IOptions<ScanServiceOptions>>());
        });
        services.AddSingleton(Options.Create(ReadIdentityOptions(configuration)));
        services.AddSingleton<IAuthTokenService, HmacAuthTokenService>();
        services.AddScoped<IUserAuthenticationService, ConfiguredUserAuthenticationService>();

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

    private static EmailOptions ReadEmailOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection("Email");
        return new EmailOptions
        {
            SmtpHost = section["SmtpHost"] ?? "",
            SmtpPort = int.TryParse(section["SmtpPort"], out var port) ? port : 587,
            SmtpUser = section["SmtpUser"] ?? "",
            SmtpPassword = section["SmtpPassword"] ?? "",
            EnableSsl = !bool.TryParse(section["EnableSsl"], out var ssl) || ssl,
            FromAddress = section["FromAddress"] ?? "noreply@easymitt.de",
            FromName = section["FromName"] ?? "EasyMitt",
        };
    }

    private static ScanServiceOptions ReadScanServiceOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection("ScanService");
        return new ScanServiceOptions
        {
            BaseUrl = section["BaseUrl"] ?? "http://127.0.0.1:7332",
            TimeoutSeconds = int.TryParse(section["TimeoutSeconds"], out var timeoutSeconds)
                ? timeoutSeconds
                : 120,
            MaxFileBytes = int.TryParse(section["MaxFileBytes"], out var maxFileBytes)
                ? maxFileBytes
                : 8 * 1024 * 1024,
        };
    }
}
