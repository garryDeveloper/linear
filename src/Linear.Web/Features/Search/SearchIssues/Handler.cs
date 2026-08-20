using Linear.Domain.Common;
using Linear.Web.Features.Search.Contracts;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Persistence;
using Linear.Web.Infrastructure.Search;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Search.SearchIssues;

/// <summary>
/// Busca issues en todos los equipos a los que pertenece el usuario.
/// </summary>
/// <remarks>
/// Es la única consulta de la aplicación que no arranca por un equipo de la ruta: el
/// buscador es global. El aislamiento lo garantiza la propia consulta, que cruza
/// <c>TeamMembers</c> con el usuario en curso — un issue de un equipo ajeno no puede
/// aparecer ni siquiera buscando su identificador exacto.
/// </remarks>
public sealed class SearchIssuesHandler(
    ICurrentUser currentUser,
    IDbContextFactory<AppDbContext> dbContextFactory)
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;

    public async Task<Result<IReadOnlyList<SearchResultResponse>>> HandleAsync(
        SearchIssuesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = await currentUser.RequireIdAsync(cancellationToken);

        if (userId.IsFailure)
        {
            return Result.Failure<IReadOnlyList<SearchResultResponse>>(userId.Error);
        }

        var term = SearchTerm.Create(request.Query);

        // Sin término utilizable no se consulta la base: es la primera de las consultas
        // innecesarias que la task pide evitar, y la que más se repetiría —el buscador se
        // abre vacío.
        if (term is null)
        {
            return Result.Success<IReadOnlyList<SearchResultResponse>>([]);
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var rows = await IssueSearchQuery.ExecuteAsync(
            dbContext, term, userId.Value, NormalizeLimit(request.Limit), cancellationToken);

        return Result.Success<IReadOnlyList<SearchResultResponse>>(
            [.. rows.Select(row => new SearchResultResponse(
                row.Id,
                row.Identifier,
                row.Title,
                row.TeamKey,
                row.TeamName,
                row.Status,
                row.MatchedInComment))]);
    }

    private static int NormalizeLimit(int limit) => limit switch
    {
        < 1 => DefaultLimit,
        > MaxLimit => MaxLimit,
        _ => limit
    };
}
