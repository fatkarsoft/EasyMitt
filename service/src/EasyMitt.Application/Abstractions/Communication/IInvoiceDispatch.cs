using EasyMitt.Application.Dtos.En16931;

namespace EasyMitt.Application.Abstractions.Communication;

/// <summary>
/// Peppol / erişim noktası üzerinden fatura iletimi (ileride gerçek AP implementasyonu).
/// </summary>
public interface IInvoiceDispatch
{
    Task<InvoiceDispatchReceipt> SubmitAsync(InvoiceDispatchRequest request, CancellationToken cancellationToken);
}

public sealed record InvoiceDispatchRequest(
    InvoiceDocumentDto Document,
    byte[] Payload,
    string PayloadContentType,
    string? RecipientEndpointId);

public sealed record InvoiceDispatchReceipt(string DispatchId, string Status, IReadOnlyDictionary<string, string>? Metadata);
