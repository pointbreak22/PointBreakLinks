using MediatR;

namespace Application.CQRS.Moderation.Commands.RejectSite;

public record RejectSiteCommand(int SiteId, int ModeratorId, string? Reason = null) : IRequest;
