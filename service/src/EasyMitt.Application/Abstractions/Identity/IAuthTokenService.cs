using System.Security.Claims;
using EasyMitt.Application.Dtos.Identity;

namespace EasyMitt.Application.Abstractions.Identity;

public interface IAuthTokenService
{
    string CreateToken(AuthenticatedUserDto user, DateTimeOffset expiresAtUtc);

    ClaimsPrincipal? ValidateToken(string token);
}
