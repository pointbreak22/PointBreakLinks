using Identity.Application.Common;
using Identity.Application.CQRS.Auth.DTOs;
using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    // Deliberately does NOT go through TokenIssuer: FOXLinks' /auth/refresh mints a new
    // access token but reuses the same refresh-token value, just pushing its expiry out
    // another 30 days — the client never needs to update the httpOnly cookie itself.
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var token = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (token == null || token.IsExpired)
        {
            throw new AuthenticationException("Invalid or expired refresh token.");
        }

        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken)
                   ?? throw new AuthenticationException("Invalid or expired refresh token.");

        // Cuts an already-logged-in session off on its next silent refresh — a ban applied
        // mid-session would otherwise do nothing until the access token naturally expired.
        if (user.IsBanned)
        {
            throw new AuthenticationException("Аккаунт заблокирован.");
        }

        token.ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

        var roles = user.Roles.Select(r => r.Name).ToArray();
        var accessToken = jwtTokenGenerator.CreateAccessToken(user, roles);
        var userDto = new UserDto(user.Id, user.Name, user.Email, roles, user.TwoFactorEnabled);

        return new AuthResultDto(accessToken, token.Token, token.ExpiresAt, userDto);
    }
}
