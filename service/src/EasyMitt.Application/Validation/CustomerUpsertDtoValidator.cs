using EasyMitt.Application.Dtos.Customers;
using EasyMitt.Application.Localization;
using FluentValidation;

namespace EasyMitt.Application.Validation;

public sealed class CustomerUpsertDtoValidator : AbstractValidator<CustomerUpsertDto>
{
    public CustomerUpsertDtoValidator()
    {
        RuleFor(x => x.Type)
            .Must(x => x is "Business" or "Consumer")
            .WithErrorCode(ValidationErrorCodes.Required);

        When(x => x.Type == "Business", () =>
        {
            RuleFor(x => x.CompanyName)
                .NotEmpty()
                .WithErrorCode(ValidationErrorCodes.Required);
        });

        When(x => x.Type == "Consumer", () =>
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithErrorCode(ValidationErrorCodes.Required);
            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithErrorCode(ValidationErrorCodes.Required);
        });

        RuleFor(x => x.Email).MaximumLength(256).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.Phone).MaximumLength(64).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.CompanyName).MaximumLength(256).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.FirstName).MaximumLength(128).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.LastName).MaximumLength(128).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.CountryCode).MaximumLength(2).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.VatId).MaximumLength(32).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.TaxNumber).MaximumLength(64).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.LeitwegId).MaximumLength(64).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.DatevDebitorAccount).MaximumLength(16).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.PaymentTermsDays).GreaterThanOrEqualTo(0).WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualZero);
    }
}
