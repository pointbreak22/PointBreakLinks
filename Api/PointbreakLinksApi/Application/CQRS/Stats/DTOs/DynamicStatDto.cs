using Domain.Entities;

namespace Application.CQRS.Stats.DTOs;

public record StatTrendDto(string? Type, string? Prefix, string? Value, string? Suffix, string? Text);

// Mirrors FOXLinks' DynamicStatResource exactly — nested `trend` object included even when
// every field inside it is null, same as the source.
public record DynamicStatDto(int Id, string PageKey, int Position, string Title, string Value, string? ValueSuffix, StatTrendDto Trend)
{
    public static DynamicStatDto FromEntity(DynamicStat stat) => new(
        stat.Id,
        stat.PageKey,
        stat.Position,
        stat.Title,
        stat.Value,
        stat.ValueSuffix,
        new StatTrendDto(stat.TrendType, stat.TrendPrefix, stat.TrendValue, stat.TrendSuffix, stat.TrendText));
}
