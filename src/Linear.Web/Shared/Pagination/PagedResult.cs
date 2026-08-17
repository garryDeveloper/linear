namespace Linear.Web.Shared.Pagination;

/// <summary>
/// Página de resultados junto con la información necesaria para navegar el listado.
/// </summary>
public sealed record PagedResult<TItem>
{
    public required IReadOnlyList<TItem> Items { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    /// <summary>Total de elementos que cumplen el filtro, no solo los de esta página.</summary>
    public required int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    public static PagedResult<TItem> Create(IReadOnlyList<TItem> items, PageRequest request, int totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

        return new PagedResult<TItem>
        {
            Items = items,
            Page = request.EffectivePage,
            PageSize = request.EffectivePageSize,
            TotalCount = totalCount
        };
    }

    public static PagedResult<TItem> Empty(PageRequest request) =>
        Create([], request, 0);
}
