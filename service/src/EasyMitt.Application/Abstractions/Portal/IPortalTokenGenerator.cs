namespace EasyMitt.Application.Abstractions.Portal;

public interface IPortalTokenGenerator
{
    PortalGeneratedToken Generate();

    string HashToken(string token);
}

public sealed record PortalGeneratedToken(string Token, string TokenHash, string TokenPrefix);
