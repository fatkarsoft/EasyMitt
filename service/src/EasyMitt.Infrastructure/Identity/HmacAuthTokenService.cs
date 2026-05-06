using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EasyMitt.Application.Abstractions.Identity;
using EasyMitt.Application.Dtos.Identity;
using EasyMitt.Domain.Localization;
using Microsoft.Extensions.Options;

namespace EasyMitt.Infrastructure.Identity;

public sealed class HmacAuthTokenService(IOptions<ConfiguredIdentityOptions> options) : IAuthTokenService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string CreateToken(AuthenticatedUserDto user, DateTimeOffset expiresAtUtc)
    {
        var payload = new TokenPayload(
            user.UserId,
            user.Email,
            user.DisplayName,
            user.CompanyId,
            user.CompanyName,
            user.Role,
            SupportedLanguages.NormalizeOrDefault(user.Language),
            expiresAtUtc.ToUnixTimeSeconds());

        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        var payloadSegment = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signature = Base64UrlEncode(Sign(payloadSegment));
        return $"{payloadSegment}.{signature}";
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var parts = token.Split('.', 2);
        if (parts.Length != 2)
        {
            return null;
        }

        var expectedSignature = Sign(parts[0]);
        var actualSignature = Base64UrlDecode(parts[1]);
        if (!CryptographicOperations.FixedTimeEquals(expectedSignature, actualSignature))
        {
            return null;
        }

        TokenPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<TokenPayload>(
                Encoding.UTF8.GetString(Base64UrlDecode(parts[0])),
                JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (payload is null || DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= payload.Exp)
        {
            return null;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, payload.Sub.ToString()),
            new(ClaimTypes.Email, payload.Email),
            new(ClaimTypes.Name, payload.Name),
            new(ClaimTypes.Role, payload.Role),
            new("company_id", payload.CompanyId.ToString()),
            new("company_name", payload.CompanyName),
            new("language", SupportedLanguages.NormalizeOrDefault(payload.Language)),
        };

        var identity = new ClaimsIdentity(claims, "EasyMittBearer");
        return new ClaimsPrincipal(identity);
    }

    private byte[] Sign(string payloadSegment)
    {
        var signingKey = options.Value.SigningKey;
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("Authentication:SigningKey tanımlı değil.");
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadSegment));
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        var padding = base64.Length % 4;
        if (padding > 0)
        {
            base64 = base64.PadRight(base64.Length + 4 - padding, '=');
        }

        return Convert.FromBase64String(base64);
    }

    private sealed record TokenPayload(
        Guid Sub,
        string Email,
        string Name,
        Guid CompanyId,
        string CompanyName,
        string Role,
        string Language,
        long Exp);
}
