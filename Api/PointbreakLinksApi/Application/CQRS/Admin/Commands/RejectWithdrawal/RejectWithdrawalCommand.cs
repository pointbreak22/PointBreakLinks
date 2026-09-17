using MediatR;

namespace Application.CQRS.Admin.Commands.RejectWithdrawal;

public record RejectWithdrawalCommand(int RequestId, string? Comment) : IRequest;
