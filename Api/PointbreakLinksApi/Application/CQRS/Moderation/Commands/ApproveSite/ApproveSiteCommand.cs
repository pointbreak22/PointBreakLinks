using MediatR;

namespace Application.CQRS.Moderation.Commands.ApproveSite;

public record ApproveSiteCommand(int SiteId, int ModeratorId) : IRequest;
