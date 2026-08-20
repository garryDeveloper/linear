namespace Linear.Web.Infrastructure.Search;

/// <summary>
/// Nombres del esquema de búsqueda, compartidos entre las configuraciones de EF que crean
/// las columnas y la consulta SQL que las lee.
/// </summary>
/// <remarks>
/// Están en un solo lugar para que renombrar la columna no deje la consulta apuntando a una
/// que ya no existe: es SQL escrito a mano, así que el compilador no avisaría.
/// </remarks>
public static class SearchSchema
{
    /// <summary>Columna <c>tsvector</c> generada, tanto en <c>Issues</c> como en <c>Comments</c>.</summary>
    public const string SearchVectorColumn = "SearchVector";

    /// <summary>
    /// Configuración de diccionario que usa la búsqueda. La crea la migración
    /// <c>AddSearch</c>.
    /// </summary>
    /// <remarks>
    /// Es 'spanish' con un paso previo de <c>unaccent</c>, no el 'spanish' de fábrica. El
    /// diccionario castellano reduce las palabras a su raíz —"autenticación" encuentra
    /// "autenticaciones"— pero no toca los acentos, y en castellano se escribe sin ellos
    /// todo el tiempo: sin este agregado, buscar "autenticacion" no encontraría
    /// "autenticación". Encadenar <c>unaccent</c> normaliza las dos puntas, así que da lo
    /// mismo cómo se escriba lo buscado y cómo se haya escrito lo guardado.
    ///
    /// <c>unaccent</c> es una extensión que viene con PostgreSQL, no un motor de búsqueda
    /// aparte: la task 009 descarta Elasticsearch y similares, no las extensiones del propio
    /// motor.
    ///
    /// Va escrito como literal en cada expresión: es lo que la vuelve IMMUTABLE y permite
    /// guardarla en una columna generada. <c>unaccent()</c> suelta no lo sería —depende del
    /// diccionario instalado—, y por eso se usa envuelto en una configuración y no como
    /// llamada directa.
    /// </remarks>
    public const string Configuration = "spanish_unaccent";
}
