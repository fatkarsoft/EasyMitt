using EasyMitt.Application.Dtos.En16931;
using EasyMitt.Application.Localization;
using FluentValidation;

namespace EasyMitt.Application.Validation;

public sealed class InvoiceCoreDtoValidator : AbstractValidator<InvoiceCoreDto>
{
    public InvoiceCoreDtoValidator()
    {
        RuleFor(x => x.InvoiceNumber)
            .NotEmpty().WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(64).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.IssueDate)
            .NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithErrorCode(ValidationErrorCodes.Required)
            .Length(3).WithErrorCode(ValidationErrorCodes.ExactLength);
        RuleFor(x => x.BuyerReference)
            .NotEmpty().WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(128).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.TaxAmount)
            .GreaterThanOrEqualTo(0).WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualZero);
        RuleFor(x => x.InvoiceTotalVatIncluded)
            .GreaterThanOrEqualTo(0).WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualZero);
    }
}
