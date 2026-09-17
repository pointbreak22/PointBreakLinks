using MediatR;

namespace Application.CQRS.Admin.Commands.ApproveWithdrawal;

// Money already left the wallet the moment the request was created
// (RequestWithdrawalCommandHandler) — this just records that the admin actually sent it
// (manually, outside the app) and notifies the user.
public record ApproveWithdrawalCommand(int RequestId) : IRequest;
