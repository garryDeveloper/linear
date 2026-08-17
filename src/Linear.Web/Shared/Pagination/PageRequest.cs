namespace Linear.Web.Shared.Pagination;

/// <summary>
/// Parámetros de paginación de un listado.
/// La paginación es obligatoria en todos los listados (.ai/architecture.md § Performance Guidelines),
/// por eso los valores fuera de rango se normalizan en lugar de rechazarse: un listado
/// nunca debe terminar devolviendo la tabla completa por un parámetro mal formado.
/// </summary>
public sealed record PageRequest
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    /// <summary>Número de página solicitado, empezando en 1.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Cantidad de elementos por página solicitada.</summary>
    public int PageSize { get; init; } = DefaultPageSize;

    /// <summary>Página efectiva, ya normalizada.</summary>
    public int EffectivePage => Page < 1 ? 1 : Page;

    /// <summary>Tamaño de página efectivo, ya acotado a <see cref="MaxPageSize"/>.</summary>
    public int EffectivePageSize => PageSize switch
    {
        < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => PageSize
    };

    /// <summary>Elementos a saltear en la consulta.</summary>
    public int Skip => (EffectivePage - 1) * EffectivePageSize;

    /// <summary>Elementos a tomar en la consulta.</summary>
    public int Take => EffectivePageSize;
}
