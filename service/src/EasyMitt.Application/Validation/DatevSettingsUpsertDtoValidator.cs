using EasyMitt.Application.Dtos.Datev;
using FluentValidation;

namespace EasyMitt.Application.Validation;

public sealed class DatevSettingsUpsertDtoValidator : AbstractValidator<DatevSettingsUpsertDto>
{
    public DatevSettingsUpsertDtoValidator()
    {
        RuleFor(x => x.ExportFormat)
            .Must(x => x is "BasicCsv" or "DatevExtf")
            .WithErrorCode("validation.datev_export_format");

        RuleFor(x => x.ChartOfAccounts)
            .Must(x => x is "SKR03" or "SKR04")
            .WithErrorCode("validation.datev_chart");

        RuleFor(x => x.RevenueAccount).NotEmpty().MaximumLength(16).WithErrorCode("validation.required");
        RuleFor(x => x.DefaultExpenseAccount).NotEmpty().MaximumLength(16).WithErrorCode("validation.required");
        RuleFor(x => x.CustomerContraAccount).NotEmpty().MaximumLength(16).WithErrorCode("validation.required");
        RuleFor(x => x.VendorContraAccount).NotEmpty().MaximumLength(16).WithErrorCode("validation.required");
        RuleFor(x => x.ConsultantNumber).MaximumLength(32).WithErrorCode("validation.max_length");
        RuleFor(x => x.ClientNumber).MaximumLength(32).WithErrorCode("validation.max_length");

        RuleForEach(x => x.ExpenseAccountMappings).ChildRules(mapping =>
        {
            mapping.RuleFor(x => x.Category).NotEmpty().MaximumLength(80).WithErrorCode("validation.required");
            mapping.RuleFor(x => x.Account).NotEmpty().MaximumLength(16).WithErrorCode("validation.required");
        });

        RuleForEach(x => x.TaxKeyMappings).ChildRules(mapping =>
        {
            mapping.RuleFor(x => x.Source)
                .Must(x => x is "Invoice" or "Expense")
                .WithErrorCode("validation.required");
            mapping.RuleFor(x => x.VatRate).GreaterThanOrEqualTo(0).WithErrorCode("validation.greater_than_or_equal_zero");
            mapping.RuleFor(x => x.TaxKey).MaximumLength(16).WithErrorCode("validation.max_length");
        });
    }
}
