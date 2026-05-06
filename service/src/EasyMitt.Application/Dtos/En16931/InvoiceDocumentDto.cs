namespace EasyMitt.Application.Dtos.En16931;

public sealed class InvoiceDocumentDto
{
    public InvoiceCoreDto Core { get; init; } = new();

    public SellerPartyDto Seller { get; init; } = new();

    public BuyerPartyDto Buyer { get; init; } = new();

    public IReadOnlyList<InvoiceLineDto> Lines { get; init; } = Array.Empty<InvoiceLineDto>();
}
