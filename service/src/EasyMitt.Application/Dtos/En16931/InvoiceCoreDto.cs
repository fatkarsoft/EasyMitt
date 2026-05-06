using System.Text.Json.Serialization;

namespace EasyMitt.Application.Dtos.En16931;

public sealed class InvoiceCoreDto
{
    [JsonPropertyName("BT-1")]
    public string InvoiceNumber { get; init; } = "";

    [JsonPropertyName("BT-2")]
    public DateOnly IssueDate { get; init; }

    [JsonPropertyName("BT-5")]
    public string CurrencyCode { get; init; } = "EUR";

    [JsonPropertyName("BT-10")]
    public string BuyerReference { get; init; } = "";

    [JsonPropertyName("BT-110")]
    public decimal TaxAmount { get; init; }

    [JsonPropertyName("BT-112")]
    public decimal InvoiceTotalVatIncluded { get; init; }
}
