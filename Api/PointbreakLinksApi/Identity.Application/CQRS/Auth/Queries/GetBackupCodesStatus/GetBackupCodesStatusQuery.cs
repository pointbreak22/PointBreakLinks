using MediatR;

namespace Identity.Application.CQRS.Auth.Queries.GetBackupCodesStatus;

public record GetBackupCodesStatusQuery(int UserId) : IRequest<int>;
