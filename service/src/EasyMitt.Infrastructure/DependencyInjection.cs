using EasyMitt.Application.Abstractions.Ai;
using EasyMitt.Application.Abstractions.Archiving;
using EasyMitt.Application.Abstractions.Communication;
using EasyMitt.Application.Abstractions.Compliance;
using EasyMitt.Application.Abstractions.Email;
using EasyMitt.Application.Abstractions.Identity;
using EasyMitt.Application.Abstractions.Jobs;
using EasyMitt.Application.Abstractions.Persistence;
using EasyMitt.Application.Abstractions.Portal;
using EasyMitt.Application.Abstractions.Transformation;
using EasyMitt.Infrastructure.Ai;
using EasyMitt.Infrastructure.Archiving;
using EasyMitt.Infrastructure.Communication;
using EasyMitt.Infrastructure.Diagnostics;
using EasyMitt.Infrastructure.ElectronicInvoicing;
using EasyMitt.Infrastructure.Email;
using EasyMitt.Infrastructure.Identity;
using EasyMitt.Infrastructure.Ingestion;
using EasyMitt.Infrastructure.Jobs;
using EasyMitt.Infrastructure.Persistence;
using EasyMitt.Infrastructure.Persistence.Repositories;
using EasyMitt.Infrastructure.Portal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

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
        services.AddScoped<IDispatchLogRepository, DispatchLogRepository>();
        services.AddSingleton<IPortalTokenGenerator, PortalTokenGenerator>();
        services.AddScoped<IAiSuggestionRepository, AiSuggestionRepository>();
        services.AddSingleton<IExpenseCategorySuggester, ExpenseCategorySuggester>();
        services.AddScoped<IDatevAccountSuggester, DatevAccountSuggester>();
        services.AddScoped<IPaymentMatchScorer, PaymentMatchScorerService>();
        services.AddScoped<IMissingFieldSuggester, MissingFieldSuggester>();

        // Email — backend selector
        var emailOptions = ReadEmailOptions(configuration);
        services.AddSingleton(Options.Create(emailOptions));
        var resolvedEmailBackend = ResolveEmailBackend(emailOptions);
        services.AddHttpClient<PostmarkEmailService>();
        switch (resolvedEmailBackend)
        {
            case "Postmark":
                services.AddSingleton<IEmailService>(sp => sp.GetRequiredService<PostmarkEmailService>());
                break;
            case "Smtp":
                services.AddSingleton<IEmailService, SmtpEmailService>();
                break;
            default:
                services.AddSingleton<IEmailService, NoOpEmailService>();
                break;
        }

        // Archive — backend selector
        var archiveOptions = ReadArchiveOptions(configuration);
        services.AddSingleton(Options.Create(archiveOptions));
        services.AddSingleton<IImmutableArchiveStore>(sp =>
        {
            if (string.Equals(archiveOptions.Backend, "S3", StringComparison.OrdinalIgnoreCase) && archiveOptions.S3.IsConfigured)
            {
                var logger = sp.GetRequiredService<ILogger<S3ObjectLockArchiveStore>>();
                return new S3ObjectLockArchiveStore(archiveOptions.S3, logger);
            }

            var configuredArchiveRoot = archiveOptions.LocalRoot;
            var archiveRoot = string.IsNullOrWhiteSpace(configuredArchiveRoot)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasyMitt", "archive")
                : configuredArchiveRoot;
            return new LocalFileImmutableArchiveStore(archiveRoot);
        });
        services.AddScoped<IArchiveVerifier, ArchiveVerifier>();

        services.AddSingleton<IInvoiceSchematronValidator, InvoiceSchematronValidator>();
        services.AddSingleton<IElectronicInvoiceGenerator, S2ElectronicInvoiceGenerator>();

        // Dispatch — backend selector
        var dispatchOptions = ReadDispatchOptions(configuration);
        services.AddSingleton(Options.Create(dispatchOptions));
        services.AddHttpClient<PartnerGatewayInvoiceDispatch>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(Math.Max(dispatchOptions.PartnerGateway.TimeoutSeconds, 1));
        });
        if (string.Equals(dispatchOptions.Backend, "PartnerGateway", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IInvoiceDispatch>(sp => sp.GetRequiredService<PartnerGatewayInvoiceDispatch>());
        else
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

        // Background jobs
        var jobOptions = ReadJobOptions(configuration);
        services.AddSingleton(Options.Create(jobOptions));
        services.AddSingleton<JobRunHistory>();
        services.AddTransient<EmailRetryJob>();
        services.AddTransient<OverdueInvoiceJob>();
        services.AddTransient<DatevExportScheduledJob>();

        var registrations = new List<JobRegistration>
        {
            new(EmailRetryJob.Name, "Failed e-postaları yeniden dener (3x, exp backoff).", "0 */15 * * * ?", jobOptions.EmailRetryEnabled, typeof(EmailRetryJob)),
            new(OverdueInvoiceJob.Name, "Vadeyi geçmiş Issued/Sent faturayı Overdue yapar (günlük).", "0 0 4 * * ?", jobOptions.OverdueInvoiceEnabled, typeof(OverdueInvoiceJob)),
            new(DatevExportScheduledJob.Name, "Aylık DATEV export periyot kapama (opsiyonel, varsayılan kapalı).", jobOptions.DatevExportCron, jobOptions.DatevExportScheduledEnabled, typeof(DatevExportScheduledJob)),
        };
        services.AddSingleton<IReadOnlyList<JobRegistration>>(registrations);
        services.AddSingleton<IJobRegistry, JobRegistry>();

        services.AddQuartz(q =>
        {
            foreach (var reg in registrations)
            {
                if (!reg.Enabled) continue;
                var key = new JobKey(reg.Name);
                q.AddJob(reg.JobType, key, configure: c => c.WithIdentity(key).StoreDurably());
                q.AddTrigger(t => t
                    .ForJob(key)
                    .WithIdentity($"{reg.Name}-trigger")
                    .WithCronSchedule(reg.Schedule, x => x.WithMisfireHandlingInstructionDoNothing()));
            }
        });
        services.AddQuartzHostedService(o => o.WaitForJobsToComplete = false);

        // Health checks
        services.AddScoped<DatabaseHealthCheck>();
        services.AddScoped<ArchiveHealthCheck>();
        services.AddScoped<SecretsHealthCheck>();
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" })
            .AddCheck<ArchiveHealthCheck>("archive", tags: new[] { "ready" })
            .AddCheck<SecretsHealthCheck>("secrets", tags: new[] { "ready" });

        return services;
    }

    private static string ResolveEmailBackend(EmailOptions options)
    {
        if (string.Equals(options.Backend, "Postmark", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(options.Postmark.ServerToken))
            return "Postmark";
        if (string.Equals(options.Backend, "Smtp", StringComparison.OrdinalIgnoreCase) && options.IsSmtpConfigured)
            return "Smtp";
        if (string.Equals(options.Backend, "Auto", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(options.Backend))
        {
            if (!string.IsNullOrWhiteSpace(options.Postmark.ServerToken)) return "Postmark";
            if (options.IsSmtpConfigured) return "Smtp";
        }
        return "NoOp";
    }

    private static ConfiguredIdentityOptions ReadIdentityOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection("Authentication");
        var users = section.GetSection("Users")
            .GetChildren()
            .Where(child => !string.IsNullOrWhiteSpace(child["UserId"]) && !string.IsNullOrWhiteSpace(child["CompanyId"]))
            .Select(user => new ConfiguredUserOptions
            {
                UserId = Guid.Parse(user["UserId"]!),
                Email = user["Email"] ?? "",
                Password = user["Password"] ?? "",
                DisplayName = user["DisplayName"] ?? "",
                CompanyId = Guid.Parse(user["CompanyId"]!),
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
        var postmarkSection = section.GetSection("Postmark");
        return new EmailOptions
        {
            Backend = section["Backend"] ?? "Auto",
            SmtpHost = section["SmtpHost"] ?? "",
            SmtpPort = int.TryParse(section["SmtpPort"], out var port) ? port : 587,
            SmtpUser = section["SmtpUser"] ?? "",
            SmtpPassword = section["SmtpPassword"] ?? "",
            EnableSsl = !bool.TryParse(section["EnableSsl"], out var ssl) || ssl,
            FromAddress = section["FromAddress"] ?? "noreply@easymitt.de",
            FromName = section["FromName"] ?? "EasyMitt",
            Postmark = new PostmarkEmailOptions
            {
                ServerToken = postmarkSection["ServerToken"] ?? "",
                MessageStream = postmarkSection["MessageStream"] ?? "outbound",
            },
        };
    }

    private static ArchiveOptions ReadArchiveOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection("Archive");
        var s3Section = section.GetSection("S3");
        return new ArchiveOptions
        {
            Backend = section["Backend"] ?? "Local",
            LocalRoot = section["LocalRoot"] ?? "",
            S3 = new S3ArchiveOptions
            {
                BucketName = s3Section["BucketName"] ?? "",
                Region = s3Section["Region"] ?? "eu-central-1",
                AccessKeyId = s3Section["AccessKeyId"] ?? "",
                SecretAccessKey = s3Section["SecretAccessKey"] ?? "",
                ObjectLockRetentionDays = int.TryParse(s3Section["ObjectLockRetentionDays"], out var days) ? days : 3650,
            },
        };
    }

    private static DispatchOptions ReadDispatchOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection("Dispatch");
        var gw = section.GetSection("PartnerGateway");
        return new DispatchOptions
        {
            Backend = section["Backend"] ?? "NoOp",
            PartnerGateway = new PartnerGatewayOptions
            {
                BaseUrl = gw["BaseUrl"] ?? "",
                ApiKey = gw["ApiKey"] ?? "",
                ParticipantId = gw["ParticipantId"] ?? "",
                TimeoutSeconds = int.TryParse(gw["TimeoutSeconds"], out var timeout) ? timeout : 30,
            },
        };
    }

    private static JobOptions ReadJobOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection("Jobs");
        return new JobOptions
        {
            EmailRetryEnabled = !bool.TryParse(section["EmailRetryEnabled"], out var er) || er,
            OverdueInvoiceEnabled = !bool.TryParse(section["OverdueInvoiceEnabled"], out var oi) || oi,
            DatevExportScheduledEnabled = bool.TryParse(section["DatevExportScheduledEnabled"], out var de) && de,
            DatevExportCron = section["DatevExportCron"] ?? "0 0 3 1 * ?",
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
