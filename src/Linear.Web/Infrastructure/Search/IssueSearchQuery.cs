using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Infrastructure.Search;

/// <summary>
/// La consulta de búsqueda global de issues.
/// </summary>
/// <remarks>
/// Un issue puede aparecer por tres motivos: porque su identificador empieza como lo que se
/// escribió, porque su título o descripción coinciden, o porque lo dice alguno de sus
/// comentarios. Los tres se resuelven como ramas separadas de un <c>UNION ALL</c> y recién
/// después se juntan.
///
/// La forma importa. Escribirlo como una sola consulta con las tres condiciones unidas por
/// <c>OR</c> parece más simple, pero obliga a recorrer todos los issues del usuario: con un
/// <c>OR</c> de por medio el planificador no puede entrar por ningún índice, y la búsqueda
/// del comentario —que va correlacionada— termina ejecutándose una vez por issue. Medido
/// con 50.000 issues, esa versión recorría los 10.000 del usuario y evaluaba 10.000 veces la
/// subconsulta de comentarios. Con las ramas separadas, cada una entra por su índice y
/// devuelve un puñado de candidatos; el filtro por equipo, el orden y el recorte trabajan
/// sobre ese puñado.
///
/// El orden final es deliberado: primero el identificador —quien escribe "WEB-12" está
/// pidiendo ese issue, no uno que lo mencione—, después la relevancia de <c>ts_rank</c>, que
/// ya incorpora los pesos del título y la descripción, y a igualdad lo más nuevo primero.
///
/// Los parámetros se repiten en cada rama en lugar de calcularse una vez en un CTE. Con el
/// CTE de por medio el planificador pierde de vista los valores y deja de usar los índices:
/// el patrón del identificador terminaba en un recorrido secuencial, y el <c>JOIN</c> final
/// leía todos los issues del usuario en vez de arrancar por el puñado que coincidió.
///
/// El nombre de la configuración lleva <c>::regconfig</c> porque <c>FromSql</c> convierte
/// cada interpolación en un parámetro, no en texto literal: sin el casteo, PostgreSQL
/// recibe un <c>text</c> donde espera una configuración y rechaza la consulta.
/// </remarks>
public static class IssueSearchQuery
{
    /// <summary>
    /// Puntaje fijo de una coincidencia por identificador. Está por encima de cualquier
    /// <c>ts_rank</c>, que siempre devuelve menos de 1.
    /// </summary>
    private const string IdentifierRank = "2.0";

    /// <summary>
    /// Los archivados quedan afuera, igual que en el listado de issues: la búsqueda mira el
    /// trabajo vigente del equipo.
    /// </summary>
    public static Task<List<SearchResultRow>> ExecuteAsync(
        AppDbContext dbContext,
        SearchTerm term,
        Guid userId,
        int limit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(term);

        var tsQuery = term.TsQuery;
        var identifier = term.IdentifierPrefix;

        return dbContext.Set<SearchResultRow>()
            .FromSql(
                $"""
                 WITH matches AS (
                     -- Por identificador: entra por IX_Issues_Identifier_Prefix.
                     SELECT i."Id" AS "IssueId", {IdentifierRank}::real AS "Rank", false AS "FromComment"
                     FROM "Issues" i
                     WHERE i."Identifier" LIKE {identifier} ESCAPE '\'

                     UNION ALL

                     -- Por título o descripción: entra por el índice GIN de Issues.
                     SELECT i."Id", ts_rank(i."SearchVector", to_tsquery({SearchSchema.Configuration}::regconfig, {tsQuery})), false
                     FROM "Issues" i
                     WHERE i."SearchVector" @@ to_tsquery({SearchSchema.Configuration}::regconfig, {tsQuery})

                     UNION ALL

                     -- Por comentarios: entra por el índice GIN de Comments. Se queda con el
                     -- mejor comentario de cada issue, para no repetirlo una vez por comentario.
                     SELECT c."IssueId", MAX(ts_rank(c."SearchVector", to_tsquery({SearchSchema.Configuration}::regconfig, {tsQuery}))), true
                     FROM "Comments" c
                     WHERE c."SearchVector" @@ to_tsquery({SearchSchema.Configuration}::regconfig, {tsQuery})
                       AND c."DeletedAt" IS NULL
                     GROUP BY c."IssueId"
                 ),
                 ranked AS (
                     SELECT
                         "IssueId",
                         MAX("Rank")        AS "Rank",
                         -- Solo cuenta como "coincide en un comentario" si ninguna otra rama
                         -- lo encontró: si el título ya coincidía, no hay nada que explicar.
                         bool_and("FromComment") AS "OnlyComment"
                     FROM matches
                     GROUP BY "IssueId"
                 )
                 SELECT
                     i."Id"              AS "Id",
                     i."Identifier"      AS "Identifier",
                     i."Title"           AS "Title",
                     t."Key"             AS "TeamKey",
                     t."Name"            AS "TeamName",
                     i."Status"          AS "Status",
                     r."OnlyComment"     AS "MatchedInComment"
                 FROM ranked r
                 INNER JOIN "Issues" i
                     ON i."Id" = r."IssueId"
                 INNER JOIN "Teams" t
                     ON t."Id" = i."TeamId"
                 INNER JOIN "TeamMembers" tm
                     ON tm."TeamId" = i."TeamId" AND tm."UserId" = {userId}
                 WHERE i."ArchivedAt" IS NULL
                 ORDER BY r."Rank" DESC, i."CreatedAt" DESC
                 LIMIT {limit}
                 """)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
