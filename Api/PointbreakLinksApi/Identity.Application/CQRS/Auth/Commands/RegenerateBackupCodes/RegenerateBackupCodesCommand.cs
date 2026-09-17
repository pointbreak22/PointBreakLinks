using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.RegenerateBackupCodes;

// Invalidates every unused backup code and mints a fresh batch — for a user who used most of
// them up, or suspects an old list leaked. Password-gated like Disable2Fa, for the same reason.
public record RegenerateBackupCodesCommand(int UserId, string Password) : IRequest<IReadOnlyList<string>>;
