using Application.CQRS.SavedSearches.DTOs;
using MediatR;

namespace Application.CQRS.SavedSearches.Commands.CreateSavedSearch;

public record CreateSavedSearchCommand(
    int UserId,
    int? TopicId,
    int? CountryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    int? MinIks,
    int? MinDr) : IRequest<SavedSearchDto>;
