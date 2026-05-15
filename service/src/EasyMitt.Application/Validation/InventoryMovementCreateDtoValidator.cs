using EasyMitt.Application.Dtos.Inventory;
using EasyMitt.Application.Localization;
using FluentValidation;

namespace EasyMitt.Application.Validation;

public sealed class InventoryMovementCreateDtoValidator : AbstractValidator<InventoryMovementCreateDto>
{
    public InventoryMovementCreateDtoValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(x => x.Type)
            .Must(x => x is "OpeningBalance" or "Purchase" or "Adjustment" or "InvoiceReservation" or "InvoiceIssue")
            .WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(x => x.QuantityDelta)
            .NotEqual(0)
            .WithErrorCode(ValidationErrorCodes.GreaterThanZero);
        RuleFor(x => x.Reason).MaximumLength(512).WithErrorCode(ValidationErrorCodes.MaxLength);
    }
}
