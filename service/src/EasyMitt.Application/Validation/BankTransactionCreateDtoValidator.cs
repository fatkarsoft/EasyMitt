using EasyMitt.Application.Dtos.Payments;
using EasyMitt.Application.Localization;
using FluentValidation;

namespace EasyMitt.Application.Validation;

public sealed class BankTransactionCreateDtoValidator : AbstractValidator<BankTransactionCreateDto>
{
    public BankTransactionCreateDtoValidator()
    {
        RuleFor(x => x.BookingDate).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(x => x.Description).NotEmpty().WithErrorCode(ValidationErrorCodes.Required).MaximumLength(512).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.CounterpartyName).MaximumLength(256).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.CounterpartyIban).MaximumLength(34).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.Amount).NotEqual(0).WithErrorCode(ValidationErrorCodes.GreaterThanZero);
        RuleFor(x => x.CurrencyCode).NotEmpty().WithErrorCode(ValidationErrorCodes.Required).Length(3).WithErrorCode(ValidationErrorCodes.ExactLength);
    }
}

public sealed class PaymentAllocationCreateDtoValidator : AbstractValidator<PaymentAllocationCreateDto>
{
    public PaymentAllocationCreateDtoValidator()
    {
        RuleFor(x => x.BankTransactionId).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(x => x.InvoiceDraftId).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(x => x.Amount).GreaterThan(0).WithErrorCode(ValidationErrorCodes.GreaterThanZero);
    }
}
