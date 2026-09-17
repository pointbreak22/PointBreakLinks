using Identity.Application.Common;
using Identity.Application.CQRS.Auth.DTOs;
using Identity.Domain.Constants;
using Identity.Domain.Entities;
using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.Register;

public class RegisterCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    TokenIssuer tokenIssuer) : IRequestHandler<RegisterCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new AuthenticationException("A user with this email already exists.");
        }

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
        };

        // Every new signup starts as "universal" — mirrors FOXLinks' AuthController::register().
        var defaultRole = await userRepository.GetRoleByNameAsync(RoleNames.DefaultOnRegister, cancellationToken);
        if (defaultRole != null)
        {
            user.Roles.Add(defaultRole);
        }

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return await tokenIssuer.IssueAsync(user, cancellationToken);
    }
}
