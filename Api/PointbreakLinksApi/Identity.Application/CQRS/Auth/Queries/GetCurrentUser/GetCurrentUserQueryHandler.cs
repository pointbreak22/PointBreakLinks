using Identity.Application.CQRS.Auth.DTOs;
using Identity.Domain.Exceptions;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.CQRS.Auth.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler(IUserRepository userRepository) : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Identity.Domain.Entities.User), request.UserId);

        return new UserDto(user.Id, user.Name, user.Email, user.Roles.Select(r => r.Name).ToArray(), user.TwoFactorEnabled);
    }
}
