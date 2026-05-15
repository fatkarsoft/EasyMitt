using EasyMitt.Application.Dtos.Quotes;
using FluentValidation;

namespace EasyMitt.Application.Validation;

public sealed class QuoteUpsertDtoValidator : AbstractValidator<QuoteUpsertDto>
{
    public QuoteUpsertDtoValidator(IValidator<EasyMitt.Application.Dtos.En16931.InvoiceDocumentDto> documentValidator)
    {
        RuleFor(x => x.Document).SetValidator(documentValidator);
    }
}
