using Application.CQRS.Sites.DTOs;
using MediatR;

namespace Application.CQRS.Sites.Commands.CreateSite;

public record CreateSiteCommand(
    string Url,
    int TopicId,
    string? Description,
    decimal Price,
    int Iks,
    int Dr,
    int Traffic,
    int? CountryId,
    int SellerId) : IRequest<SiteDto>;
