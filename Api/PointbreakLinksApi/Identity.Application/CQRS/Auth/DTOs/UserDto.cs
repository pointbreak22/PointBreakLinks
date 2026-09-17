namespace Identity.Application.CQRS.Auth.DTOs;

// No Balance field here — that's business (Wallet), not identity. WebAPI's AuthController
// composes the final `user` JSON (adding balance from a business-side query) at the HTTP
// boundary, the one place it's appropriate to combine both bounded contexts — see its comment.
public record UserDto(int Id, string Name, string Email, IReadOnlyCollection<string> Roles, bool TwoFactorEnabled);
