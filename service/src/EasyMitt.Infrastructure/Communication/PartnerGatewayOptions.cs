namespace EasyMitt.Infrastructure.Communication;

public sealed class DispatchOptions
{
    public string Backend { get; init; } = "NoOp";

    public PartnerGatewayOptions PartnerGateway { get; init; } = new();
}

public sealed class PartnerGatewayOptions
{
    public string BaseUrl { get; init; } = "";
    public string ApiKey { get; init; } = "";
    public string ParticipantId { get; init; } = "";
    public int TimeoutSeconds { get; init; } = 30;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl);
}
