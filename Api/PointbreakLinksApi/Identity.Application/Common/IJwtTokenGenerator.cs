using Identity.Domain.Entities;

namespace Identity.Application.Common;

public interface IJwtTokenGenerator
{
    // Roles are passed in explicitly (rather than read off user.Roles) so callers control
    // exactly what ends up in the token without relying on lazy-loading having happened.
    string CreateAccessToken(User user, IReadOnlyCollection<string> roles);
}
