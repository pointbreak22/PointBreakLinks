using Domain.Entities;

namespace Application.CQRS.SavedSearches.DTOs;

public record SavedSearchDto(
    int Id,
    int? TopicId,
    string? TopicName,
    int? CountryId,
    string? CountryName,
    decimal? MinPrice,
    decimal? MaxPrice,
    int? MinIks,
    int? MinDr,
    string CreatedAt)
{
    // Requires Topic/Country loaded when set — see ISavedSearchRepository's comment.
    public static SavedSearchDto FromEntity(SavedSearch search) => new(
        search.Id,
        search.TopicId,
        search.Topic?.Name,
        search.CountryId,
        search.Country?.Name,
        search.MinPrice,
        search.MaxPrice,
        search.MinIks,
        search.MinDr,
        search.CreatedAt.ToString("dd.MM.yyyy"));
}
