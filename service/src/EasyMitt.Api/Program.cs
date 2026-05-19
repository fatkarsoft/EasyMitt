using EasyMitt.Api.Exceptions;
using EasyMitt.Api.Features;
using EasyMitt.Api.Localization;
using EasyMitt.Api.Responses;
using EasyMitt.Api.Security;
using EasyMitt.Application;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Localization;
using EasyMitt.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Production-style env override: EASYMITT__Section__Key=value
builder.Configuration.AddEnvironmentVariables(prefix: "EASYMITT__");

// Serilog — JSON in production, console template in development
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithProperty("Application", "EasyMitt.Api")
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Quartz", LogEventLevel.Warning);

    if (context.HostingEnvironment.IsDevelopment())
    {
        configuration.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
    }
    else
    {
        configuration.WriteTo.Console(new Serilog.Formatting.Compact.RenderedCompactJsonFormatter());
    }
});

builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentLanguage, HttpCurrentLanguage>();
builder.Services.AddScoped<ApiResponseFactory>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddAuthentication(HmacBearerAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, HmacBearerAuthenticationHandler>(
        HmacBearerAuthenticationHandler.SchemeName,
        options => { });
builder.Services.AddAuthorization(options => options.AddEasyMittPolicies());

// OpenTelemetry — etkin yalnızca OTLP endpoint set edilmişse
var otlpEndpoint = builder.Configuration["Telemetry:OtlpEndpoint"];
var serviceName = builder.Configuration["Telemetry:ServiceName"] ?? "easymitt-api";
if (!string.IsNullOrWhiteSpace(otlpEndpoint))
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(serviceName))
        .WithTracing(t => t
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(otlpEndpoint)))
        .WithMetrics(m => m
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(otlpEndpoint)));
}

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
        var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId)) diagnosticContext.Set("UserId", userId);
        var companyId = httpContext.User.FindFirst("company_id")?.Value;
        if (!string.IsNullOrEmpty(companyId)) diagnosticContext.Set("CompanyId", companyId);
    };
});

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<EasyMitt.Infrastructure.Persistence.EasyMittDbInitializer>()
        .InitializeAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs", options =>
    {
        options.Title = "EasyMitt API";
        options.OpenApiRoutePattern = "/openapi/v1.json";
    });
}

app.MapGet("/health", (
    ApiResponseFactory responseFactory,
    IAppLocalizer localizer,
    HttpContext httpContext) =>
    Results.Ok(responseFactory.Success(
        httpContext,
        localizer.Get(MessageKeys.SystemHealthy),
        new { status = "ok" })));

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthResponseWriter.WriteAsync,
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.WriteAsync,
});

app.UseAuthentication();
app.UseMiddleware<UserLanguageMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseAuthorization();
app.MapAuthEndpoints();
app.MapInvoiceEndpoints();
app.MapInvoiceSchematronEndpoints();
app.MapCustomerEndpoints();
app.MapProductEndpoints();
app.MapInventoryEndpoints();
app.MapQuoteEndpoints();
app.MapExpenseEndpoints();
app.MapPaymentEndpoints();
app.MapDunningEndpoints();
app.MapReportingEndpoints();
app.MapDatevEndpoints();
app.MapDatevSettingsEndpoints();
app.MapComplianceEndpoints();
app.MapComplianceVerifyEndpoints();
app.MapEmailEndpoints();
app.MapPortalEndpoints();
app.MapAiEndpoints();
app.MapAdminEndpoints();

// Startup secret check (logs warning if SigningKey is missing)
var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("EasyMitt.Startup");
var identityOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<EasyMitt.Infrastructure.Identity.ConfiguredIdentityOptions>>().Value;
if (string.IsNullOrWhiteSpace(identityOptions.SigningKey))
    startupLogger.LogWarning("Authentication:SigningKey eksik — login akışı çalışmayacak. EASYMITT__Authentication__SigningKey ortam değişkenini ayarlayın.");

app.Run();
