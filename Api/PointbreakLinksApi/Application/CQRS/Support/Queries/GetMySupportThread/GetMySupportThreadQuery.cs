using Application.CQRS.Support.DTOs;
using MediatR;

namespace Application.CQRS.Support.Queries.GetMySupportThread;

public record GetMySupportThreadQuery(int UserId) : IRequest<IReadOnlyList<SupportMessageDto>>;
