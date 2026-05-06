using System.Text.Json.Serialization;

namespace EasyMitt.Application.Dtos.En16931;

public sealed class SellerPartyDto
{
    [JsonPropertyName("BT-20")]
    public string Name { get; init; } = "";

    [JsonPropertyName("BT-22")]
    public string? VatId { get; init; }

    [JsonPropertyName("BT-34")]
    public string? PaymentIban { get; init; }
}
