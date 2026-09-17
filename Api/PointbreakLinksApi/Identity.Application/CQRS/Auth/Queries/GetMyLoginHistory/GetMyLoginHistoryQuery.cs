using Identity.Application.CQRS.Auth.DTOs;
using MediatR;

namespace Identity.Application.CQRS.Auth.Queries.GetMyLoginHistory;

public record GetMyLoginHistoryQuery(int UserId, int Limit = 10) : IRequest<IReadOnlyList<LoginHistoryEntryDto>>;
