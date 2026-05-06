using EasyMitt.Application.Dtos.En16931;
using EasyMitt.Application.Localization;
using EasyMitt.Domain.Taxation;
using FluentValidation;

namespace EasyMitt.Application.Validation;

public sealed class InvoiceLineDtoValidator : AbstractValidator<InvoiceLineDto>
{
    public InvoiceLineDtoValidator()
    {
        RuleFor(x => x.ItemName)
            .NotEmpty().WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(512).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithErrorCode(ValidationErrorCodes.GreaterThanZero);
        RuleFor(x => x.NetAmount)
            .GreaterThanOrEqualTo(0).WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualZero);
        RuleFor(x => x.VatRatePercent)
            .Must(GermanVatRatePolicy.IsCommonRate)
            .WithErrorCode(ValidationErrorCodes.GermanVatRate);
    }
}
