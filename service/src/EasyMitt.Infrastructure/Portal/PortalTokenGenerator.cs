using System.Security.Cryptography;
using EasyMitt.Application.Abstractions.Portal;

namespace EasyMitt.Infrastructure.Portal;

public sealed class PortalTokenGenerator : IPortalTokenGenerator
{
    public PortalGeneratedToken Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = ToUrlSafeBase64(bytes);
        var hash = HashToken(token);
        var prefix = token.Length >= 8 ? token[..8] : token;
        return new PortalGeneratedToken(token, hash, prefix);
    }

    public string HashToken(string token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ToUrlSafeBase64(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
