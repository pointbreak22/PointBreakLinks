using Domain.Common;

namespace Domain.Entities;

// Admin-configurable dashboard widget (e.g. "Total revenue" on the webmaster/optimizer/project
// pages). Value is stored as a string by design in FOXLinks, to stay format-agnostic (₽, %, ...).
public class DynamicStat : BaseEntity
{
    public string PageKey { get; set; } = string.Empty; // "webmaster" | "optimizator" | "project"
    public int Position { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? ValueSuffix { get; set; }

    public string? TrendType { get; set; } // "up" | "down" | null
    public string? TrendPrefix { get; set; } // "+" | "-"
    public string? TrendValue { get; set; }
    public string? TrendSuffix { get; set; }
    public string? TrendText { get; set; }
}
