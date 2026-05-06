using EasyMitt.Application.Dtos.En16931;
using EasyMitt.Application.Localization;
using FluentValidation;

namespace EasyMitt.Application.Validation;

public sealed class BuyerPartyDtoValidator : AbstractValidator<BuyerPartyDto>
{
    public BuyerPartyDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode(ValidationErrorCodes.Required)
            .MaximumLength(256).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.VatId)
            .MaximumLength(32).WithErrorCode(ValidationErrorCodes.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.VatId));
    }
}
