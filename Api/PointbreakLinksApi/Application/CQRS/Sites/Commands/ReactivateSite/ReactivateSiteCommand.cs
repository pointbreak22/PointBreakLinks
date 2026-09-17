using MediatR;

namespace Application.CQRS.Sites.Commands.ReactivateSite;

// The resume half of DeactivateSiteCommand — see its comment for why "remove my listing" is a
// reversible IsActive flip rather than a hard delete. Only allowed once a site has actually
// passed moderation (StatusId == "active"): a still-pending or rejected listing was never in the
// catalog to begin with, and letting a seller flip IsActive back to true themselves would be a
// way to skip moderation entirely.
public record ReactivateSiteCommand(int SiteId, int OwnerId) : IRequest;
