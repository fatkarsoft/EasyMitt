using EasyMitt.Application.Dtos.Identity;

namespace EasyMitt.Application.Abstractions.Identity;

public interface IUserAuthenticationService
{
    Task<AuthenticatedUserDto?> AuthenticateAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken);
}
