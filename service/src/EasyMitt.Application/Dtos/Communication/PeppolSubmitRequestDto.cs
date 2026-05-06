using EasyMitt.Application.Dtos.En16931;

namespace EasyMitt.Application.Dtos.Communication;

public sealed class PeppolSubmitRequestDto
{
    public InvoiceDocumentDto Document { get; init; } = new();

    public string? RecipientEndpointId { get; init; }
}
