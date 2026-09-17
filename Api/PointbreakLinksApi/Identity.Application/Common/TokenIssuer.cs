using Identity.Application.CQRS.Auth.DTOs;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;

namespace Identity.Application.Common;

// Shared by Register/Login/RefreshToken handlers so the "mint access token + upsert refresh
// token" sequence (FOXLinks' AuthController::issueTokens) lives in exactly one place.
public class TokenIssuer(IJwtTokenGenerator jwtTokenGenerator, IRefreshTokenRepository refreshTokenRepository)
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<AuthResultDto> IssueAsync(User user, CancellationToken cancellationToken)
    {
        var roles = user.Roles.Select(r => r.Name).ToArray();
        var accessToken = jwtTokenGenerator.CreateAccessToken(user, roles);

        var refreshToken = GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime);
        await refreshTokenRepository.UpsertForUserAsync(user.Id, refreshToken, expiresAt, cancellationToken);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(user.Id, user.Name, user.Email, roles, user.TwoFactorEnabled);
        return new AuthResultDto(accessToken, refreshToken, expiresAt, userDto);
    }

    private static string GenerateRefreshToken() =>
        Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64)).ToLowerInvariant();
}
