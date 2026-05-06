using EasyMitt.Application.Dtos.En16931;
using EasyMitt.Application.Localization;
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
            .Must(iban => !string.IsNullOrWhiteSpace(iban) && LooksLikeIban(iban))
            .WithErrorCode(ValidationErrorCodes.IbanFormat)
            .MaximumLength(34).WithErrorCode(ValidationErrorCodes.MaxLength);
    }

    private static bool LooksLikeIban(string value)
    {
        var compact = value.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();
        return compact.Length is >= 15 and <= 34 && char.IsLetter(compact[0]) && char.IsLetter(compact[1]);
    }
}
