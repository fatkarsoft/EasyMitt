using System.Text.Json.Serialization;

namespace EasyMitt.Application.Dtos.En16931;

public sealed class InvoiceLineDto
{
    [JsonPropertyName("BT-126")]
    public string ItemName { get; init; } = "";

    [JsonPropertyName("BT-129")]
    public decimal Quantity { get; init; }

    [JsonPropertyName("BT-131")]
    public decimal NetAmount { get; init; }

    [JsonPropertyName("BT-151")]
    public decimal VatRatePercent { get; init; }
}
