using Identity.Application.CQRS.Auth.DTOs;
using MediatR;

namespace Identity.Application.CQRS.Auth.Queries.GetCurrentUser;

public record GetCurrentUserQuery(int UserId) : IRequest<UserDto>;
