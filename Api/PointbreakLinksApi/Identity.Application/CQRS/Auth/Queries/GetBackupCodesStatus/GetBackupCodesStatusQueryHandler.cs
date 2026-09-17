using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.CQRS.Auth.Queries.GetBackupCodesStatus;

public class GetBackupCodesStatusQueryHandler(ITwoFactorBackupCodeRepository backupCodeRepository)
    : IRequestHandler<GetBackupCodesStatusQuery, int>
{
    public Task<int> Handle(GetBackupCodesStatusQuery request, CancellationToken cancellationToken) =>
        backupCodeRepository.CountUnusedAsync(request.UserId, cancellationToken);
}
