using MediatR;

namespace Application.CQRS.Sites.Commands.VerifySite;

public record VerifySiteCommand(int SiteId, int OwnerId) : IRequest<bool>;
