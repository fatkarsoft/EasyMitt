using EasyMitt.Api.Exceptions;
using EasyMitt.Api.Features;
using EasyMitt.Api.Localization;
using EasyMitt.Api.Responses;
using EasyMitt.Api.Security;
using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Localization;
using EasyMitt.Application;
using EasyMitt.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

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
app.UseAuthentication();
app.UseMiddleware<UserLanguageMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseAuthorization();
app.MapAuthEndpoints();
app.MapInvoiceEndpoints();
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
app.MapEmailEndpoints();

app.Run();
