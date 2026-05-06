using EasyMitt.Application.Dtos.En16931;
using EasyMitt.Application.Localization;
using FluentValidation;

namespace EasyMitt.Application.Validation;

public sealed class InvoiceDocumentDtoValidator : AbstractValidator<InvoiceDocumentDto>
{
    public InvoiceDocumentDtoValidator()
    {
        RuleFor(x => x.Core).SetValidator(new InvoiceCoreDtoValidator());
        RuleFor(x => x.Seller).SetValidator(new SellerPartyDtoValidator());
        RuleFor(x => x.Buyer).SetValidator(new BuyerPartyDtoValidator());
        RuleForEach(x => x.Lines).SetValidator(new InvoiceLineDtoValidator());
        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.LinesRequired);
    }
}
