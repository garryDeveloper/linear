namespace Linear.Web.Features.Search.Contracts;

/// <summary>
/// Un issue encontrado por el buscador global.
/// </summary>
/// <remarks>
/// Lleva la clave del equipo porque la búsqueda cruza todos los equipos del usuario: sin
/// ella no se podría ni armar el enlace al issue ni distinguir dos resultados parecidos de
/// equipos distintos.
/// </remarks>
/// <param name="MatchedInComment">
/// Indica que el issue apareció por lo que dice un comentario y no por su propio título o
/// descripción. Sirve para explicar en la lista por qué está ahí un resultado que, mirando
/// solo el título, no parece tener nada que ver.
/// </param>
public sealed record SearchResultResponse(
    Guid Id,
    string Identifier,
    string Title,
    string TeamKey,
    string TeamName,
    string Status,
    bool MatchedInComment);
