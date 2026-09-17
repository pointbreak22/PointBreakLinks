namespace Application.Common;

// Same shape as FOXLinks' PaginationService output ({ items, meta: { current_page, last_page,
// per_page, total } }) — kept identical so the Angular Pagination component ported from
// components/pagination.vue needs no changes.
public record PagedResult<T>(IReadOnlyList<T> Items, int CurrentPage, int LastPage, int PerPage, int Total)
{
    public static PagedResult<T> Create(IReadOnlyList<T> items, int total, int page, int perPage) =>
        new(items, page, Math.Max(1, (int)Math.Ceiling(total / (double)perPage)), perPage, total);
}
