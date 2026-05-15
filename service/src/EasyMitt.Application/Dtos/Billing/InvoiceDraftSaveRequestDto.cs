using EasyMitt.Application.Dtos.En16931;

namespace EasyMitt.Application.Dtos.Billing;

public sealed class InvoiceDraftSaveRequestDto
{
    public InvoiceDocumentDto Document { get; init; } = new();

    public Guid? CustomerId { get; init; }

    public IReadOnlyList<Guid?> ProductIds { get; init; } = Array.Empty<Guid?>();
}
