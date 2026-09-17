using MediatR;

namespace Identity.Application.CQRS.Auth.Commands.Confirm2Fa;

// Returns the plaintext backup codes minted alongside enabling 2FA — the only moment they're
// ever visible, since only their hash is persisted (see TwoFactorBackupCode's comment).
public record Confirm2FaCommand(int UserId, string Code) : IRequest<IReadOnlyList<string>>;
