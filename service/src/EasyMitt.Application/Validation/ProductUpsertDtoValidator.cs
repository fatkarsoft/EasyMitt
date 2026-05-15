using EasyMitt.Application.Dtos.Catalog;
using EasyMitt.Application.Localization;
using EasyMitt.Domain.Taxation;
using FluentValidation;

namespace EasyMitt.Application.Validation;

public sealed class ProductUpsertDtoValidator : AbstractValidator<ProductUpsertDto>
{
    public ProductUpsertDtoValidator()
    {
        RuleFor(x => x.Type)
            .Must(x => x is "Product" or "Service")
            .WithErrorCode(ValidationErrorCodes.Required);

        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64).WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256).WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(x => x.Description).MaximumLength(1024).WithErrorCode(ValidationErrorCodes.MaxLength);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(32).WithErrorCode(ValidationErrorCodes.Required);
        RuleFor(x => x.NetPrice).GreaterThanOrEqualTo(0).WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualZero);
        RuleFor(x => x.VatRatePercent)
            .Must(GermanVatRatePolicy.IsCommonRate)
            .WithErrorCode(ValidationErrorCodes.GermanVatRate);
        RuleFor(x => x.CurrentStock).GreaterThanOrEqualTo(0).WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualZero);
        RuleFor(x => x.MinimumStock).GreaterThanOrEqualTo(0).WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualZero);
    }
}
