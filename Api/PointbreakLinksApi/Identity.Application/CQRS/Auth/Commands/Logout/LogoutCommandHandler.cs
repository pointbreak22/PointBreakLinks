using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.Logout;

public class LogoutCommandHandler(IRefreshTokenRepository refreshTokenRepository) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await refreshTokenRepository.DeleteByTokenAsync(request.RefreshToken, cancellationToken);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);
    }
}
