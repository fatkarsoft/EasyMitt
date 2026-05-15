using EasyMitt.Application.Dtos.Expenses;
using EasyMitt.Application.Localization;
using FluentValidation;

namespace EasyMitt.Application.Validation;

public sealed class ExpenseUpsertDtoValidator : AbstractValidator<ExpenseUpsertDto>
{
    public ExpenseUpsertDtoValidator()
    {
        RuleFor(x => x.VendorName).NotEmpty().WithErrorCode(ValidationErrorCodes.Required).MaximumLength(240).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.Category).NotEmpty().WithErrorCode(ValidationErrorCodes.Required).MaximumLength(80).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.CurrencyCode).NotEmpty().WithErrorCode(ValidationErrorCodes.Required).Length(3).WithErrorCode(ValidationErrorCodes.ExactLength);
        RuleFor(x => x.DatevCreditorAccount).MaximumLength(16).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.TotalAmount).GreaterThan(0).WithErrorCode(ValidationErrorCodes.GreaterThanZero);
        RuleFor(x => x.NetAmount).GreaterThanOrEqualTo(0).WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualZero);
        RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0).WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualZero);
    }
}
