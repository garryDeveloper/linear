using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Linear.Web.Infrastructure.Search;

/// <summary>
/// Fila cruda que devuelve la consulta de búsqueda.
/// </summary>
/// <remarks>
/// Es un tipo sin clave: no representa una tabla, solo la forma del resultado de un SQL
/// escrito a mano. La búsqueda se escribe en SQL —y no con LINQ— porque necesita cosas que
/// EF no sabe expresar: <c>ts_rank</c> con pesos, un <c>LATERAL</c> para quedarse con el
/// mejor comentario de cada issue, y un orden que combina las tres formas de coincidir.
/// </remarks>
public sealed class SearchResultRow
{
    public Guid Id { get; init; }

    public string Identifier { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string TeamKey { get; init; } = string.Empty;

    public string TeamName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public bool MatchedInComment { get; init; }
}

public sealed class SearchResultRowConfiguration : IEntityTypeConfiguration<SearchResultRow>
{
    public void Configure(EntityTypeBuilder<SearchResultRow> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Sin clave y sin tabla: existe solo para darle forma al resultado de FromSql, así
        // que las migraciones no tienen que crear nada.
        builder.HasNoKey();
        builder.ToView(null);
    }
}
