using EasyMitt.Application.Dtos.En16931;
using EasyMitt.Application.Localization;
using EasyMitt.Domain.Payments;
using FluentValidation;

namespace EasyMitt.Application.Validation;

public sealed class SellerPartyDtoValidator : AbstractValidator<SellerPartyDto>
{
    public SellerPartyDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(256).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.VatId)
            .NotEmpty().WithErrorCode(ValidationErrorCodes.SellerVatRequired)
            .MaximumLength(32).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.PaymentIban)
            .NotEmpty().WithErrorCode(ValidationErrorCodes.IbanRequired)
            .Must(IbanPolicy.IsValid)
            .WithErrorCode(ValidationErrorCodes.IbanFormat)
            .Must(iban => IbanPolicy.Normalize(iban).Length <= 34)
            .WithErrorCode(ValidationErrorCodes.MaxLength);
    }
}
