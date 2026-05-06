using System.Text.Json.Serialization;

namespace EasyMitt.Application.Dtos.En16931;

public sealed class BuyerPartyDto
{
    [JsonPropertyName("BT-26")]
    public string Name { get; init; } = "";

    [JsonPropertyName("BT-48")]
    public string? VatId { get; init; }
}
