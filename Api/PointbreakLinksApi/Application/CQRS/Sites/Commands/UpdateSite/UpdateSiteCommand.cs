using Application.CQRS.Sites.DTOs;
using MediatR;

namespace Application.CQRS.Sites.Commands.UpdateSite;

public record UpdateSiteCommand(
    int SiteId,
    int OwnerId,
    string Url,
    int TopicId,
    string? Description,
    decimal Price,
    int Iks,
    int Dr,
    int Traffic,
    int? CountryId) : IRequest<SiteDto>;
