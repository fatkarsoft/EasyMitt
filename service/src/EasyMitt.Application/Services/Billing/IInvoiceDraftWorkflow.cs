using EasyMitt.Application.Dtos.En16931;
using FluentValidation.Results;

namespace EasyMitt.Application.Services.Billing;

public interface IInvoiceDraftWorkflow
{
    Task<ValidationResult> ValidateAsync(InvoiceDocumentDto document, CancellationToken cancellationToken);

    Task<Guid> SaveDraftAsync(
        Guid companyId,
        InvoiceDocumentDto document,
        Guid? customerId,
        IReadOnlyList<Guid?> productIds,
        CancellationToken cancellationToken);
}
