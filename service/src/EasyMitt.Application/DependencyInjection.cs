using EasyMitt.Application.Abstractions.Localization;
using EasyMitt.Application.Services.Billing;
using EasyMitt.Application.Services.Localization;
using EasyMitt.Application.Services.Transformation;
using EasyMitt.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EasyMitt.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<InvoiceDocumentDtoValidator>();
        services.AddScoped<IInvoiceDraftWorkflow, InvoiceDraftWorkflow>();
        services.AddScoped<IAppLocalizer, DictionaryAppLocalizer>();
        services.AddSingleton<IRawInvoiceImportMapper, RawInvoiceImportMapper>();
        return services;
    }
}
