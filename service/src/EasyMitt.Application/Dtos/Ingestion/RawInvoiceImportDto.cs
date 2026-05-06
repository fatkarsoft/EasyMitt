namespace EasyMitt.Application.Dtos.Ingestion;

/// <summary>
/// AI / harici araçlardan gelen ham fatura özeti (Studio: TaxHacker benzeri ingestion).
/// </summary>
public sealed class RawInvoiceImportDto
{
    public string? MerchantOrSellerHint { get; init; }

    public string? BuyerHint { get; init; }

    public string? IbanOrPaymentHint { get; init; }

    public string? SellerVatIdHint { get; init; }

    public string? BuyerVatIdHint { get; init; }

    public string? BuyerReferenceHint { get; init; }

    public decimal? TotalAmount { get; init; }

    public string? CurrencyHint { get; init; }

    public DateOnly? IssueDateHint { get; init; }

    public IReadOnlyList<RawLineHintDto> LineHints { get; init; } = Array.Empty<RawLineHintDto>();
}

public sealed class RawLineHintDto
{
    public string? Description { get; init; }

    public decimal? Amount { get; init; }

    public decimal? VatRatePercent { get; init; }
}
