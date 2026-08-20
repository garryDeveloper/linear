using System.Linq.Expressions;

using Linear.Domain.Common;
using Linear.Domain.Issues;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Issues.Filtering;

/// <summary>
/// Traduce las condiciones de un <see cref="IssueFilterSet"/> a la consulta de issues.
/// </summary>
/// <remarks>
/// Cada campo arma su condición <em>afirmativa</em> y, si el operador excluye, se niega el
/// árbol completo con <see cref="Not"/>. Hacerlo así no es solo para no duplicar código:
/// es lo que da la semántica correcta con valores nulos. "Responsable no es Ana" incluye a
/// los issues sin responsable, porque la condición afirmativa lleva su propio
/// <c>!= null</c> y negarla entera lo vuelve verdadero para esas filas. Escribir la
/// versión negada a mano tendería a producir <c>AssigneeId != ana</c>, que en SQL descarta
/// los nulos y escondería justamente los issues sin asignar.
///
/// Ninguna condición usa <c>Include</c>: la de labels es un <c>EXISTS</c>, así que filtra
/// sin multiplicar filas y sigue siendo compatible con la paginación del listado.
/// </remarks>
public static class IssueFilterQuery
{
    private const string Me = "me";
    private const string None = "none";
    private const string LikeEscape = "\\";

    public static async Task<Result<IQueryable<Issue>>> ApplyAsync(
        IQueryable<Issue> query,
        IssueFilterSet filters,
        Guid teamId,
        ICurrentUser currentUser,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(filters);

        foreach (var filter in filters.Filters)
        {
            var applied = await ApplyOneAsync(
                query, filter, teamId, currentUser, dbContext, cancellationToken);

            if (applied.IsFailure)
            {
                return applied;
            }

            query = applied.Value;
        }

        return Result.Success(query);
    }

    private static async Task<Result<IQueryable<Issue>>> ApplyOneAsync(
        IQueryable<Issue> query,
        IssueFilter filter,
        Guid teamId,
        ICurrentUser currentUser,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var match = filter.Field switch
        {
            IssueFilterField.Status => MatchStatus(filter),
            IssueFilterField.Priority => MatchPriority(filter),
            IssueFilterField.Title => MatchTitle(filter),
            IssueFilterField.Assignee => await MatchAssigneeAsync(filter, currentUser, cancellationToken),
            IssueFilterField.CreatedBy => await MatchCreatedByAsync(filter, currentUser, cancellationToken),
            IssueFilterField.Label => await MatchLabelAsync(filter, teamId, dbContext, cancellationToken),
            IssueFilterField.Sprint => await MatchSprintAsync(filter, teamId, dbContext, cancellationToken),
            _ => Result.Failure<Expression<Func<Issue, bool>>>(
                IssueFilterErrors.OperatorNotSupported(filter.Field, filter.Operator))
        };

        if (match.IsFailure)
        {
            return Result.Failure<IQueryable<Issue>>(match.Error);
        }

        var predicate = filter.Operator.IsNegated() ? Not(match.Value) : match.Value;

        return Result.Success(query.Where(predicate));
    }

    private static Result<Expression<Func<Issue, bool>>> MatchStatus(IssueFilter filter)
    {
        var statuses = ParseEnums<IssueStatus>(filter);

        return statuses.IsFailure
            ? Result.Failure<Expression<Func<Issue, bool>>>(statuses.Error)
            : Result.Success<Expression<Func<Issue, bool>>>(
                issue => statuses.Value.Contains(issue.Status));
    }

    private static Result<Expression<Func<Issue, bool>>> MatchPriority(IssueFilter filter)
    {
        var priorities = ParseEnums<IssuePriority>(filter);

        return priorities.IsFailure
            ? Result.Failure<Expression<Func<Issue, bool>>>(priorities.Error)
            : Result.Success<Expression<Func<Issue, bool>>>(
                issue => priorities.Value.Contains(issue.Priority));
    }

    private static Result<Expression<Func<Issue, bool>>> MatchTitle(IssueFilter filter)
    {
        // Se escapan los comodines para que un '%' escrito por quien filtra sea un '%' y no
        // "cualquier cosa". ILike es de Npgsql: compara sin distinguir mayúsculas en el motor.
        var pattern = $"%{Escape(filter.Values[0])}%";

        return Result.Success<Expression<Func<Issue, bool>>>(
            issue => EF.Functions.ILike(issue.Title, pattern, LikeEscape));
    }

    private static async Task<Result<Expression<Func<Issue, bool>>>> MatchAssigneeAsync(
        IssueFilter filter,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var users = await ParseUsersAsync(filter, currentUser, allowNone: true, cancellationToken);

        if (users.IsFailure)
        {
            return Result.Failure<Expression<Func<Issue, bool>>>(users.Error);
        }

        var (ids, includesNone) = users.Value;

        // El '!= null' explícito es lo que hace que negar esta condición incluya a los
        // issues sin responsable, en vez de descartarlos como haría SQL con un NULL.
        return Result.Success<Expression<Func<Issue, bool>>>((ids.Count, includesNone) switch
        {
            (0, _) => issue => issue.AssigneeId == null,
            (_, false) => issue => issue.AssigneeId != null && ids.Contains(issue.AssigneeId.Value),
            (_, true) => issue =>
                issue.AssigneeId == null || (issue.AssigneeId != null && ids.Contains(issue.AssigneeId.Value))
        });
    }

    private static async Task<Result<Expression<Func<Issue, bool>>>> MatchCreatedByAsync(
        IssueFilter filter,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        // Sin 'none': todo issue tiene autor, así que "creado por nadie" no existe.
        var users = await ParseUsersAsync(filter, currentUser, allowNone: false, cancellationToken);

        if (users.IsFailure)
        {
            return Result.Failure<Expression<Func<Issue, bool>>>(users.Error);
        }

        var ids = users.Value.Ids;

        return Result.Success<Expression<Func<Issue, bool>>>(
            issue => ids.Contains(issue.CreatedById));
    }

    private static async Task<Result<Expression<Func<Issue, bool>>>> MatchLabelAsync(
        IssueFilter filter,
        Guid teamId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var ids = await ResolveLabelsAsync(filter, teamId, dbContext, cancellationToken);

        if (ids.IsFailure)
        {
            return Result.Failure<Expression<Func<Issue, bool>>>(ids.Error);
        }

        var labelIds = ids.Value;

        // EXISTS y no JOIN: filtra sin duplicar el issue por cada label que tenga.
        return Result.Success<Expression<Func<Issue, bool>>>(
            issue => issue.Labels.Any(label => labelIds.Contains(label.LabelId)));
    }

    private static async Task<Result<Expression<Func<Issue, bool>>>> MatchSprintAsync(
        IssueFilter filter,
        Guid teamId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();
        var includesNone = false;

        foreach (var value in filter.Values)
        {
            if (value.Equals(None, StringComparison.OrdinalIgnoreCase))
            {
                includesNone = true;
            }
            else if (Guid.TryParse(value, out var sprintId))
            {
                ids.Add(sprintId);
            }
            else
            {
                return Result.Failure<Expression<Func<Issue, bool>>>(
                    IssueFilterErrors.UnknownValue(filter.Field, value));
            }
        }

        // Se comprueba que los sprints sean de este equipo: filtrar por el sprint de otro
        // equipo no debería devolver nada, pero tampoco confirmar que ese sprint existe.
        if (ids.Count > 0)
        {
            var known = await dbContext.Sprints
                .AsNoTracking()
                .Where(sprint => sprint.TeamId == teamId && ids.Contains(sprint.Id))
                .Select(sprint => sprint.Id)
                .ToListAsync(cancellationToken);

            ids = known;
        }

        return Result.Success<Expression<Func<Issue, bool>>>((ids.Count, includesNone) switch
        {
            (0, true) => issue => issue.SprintId == null,
            (0, false) => _ => false,
            (_, false) => issue => issue.SprintId != null && ids.Contains(issue.SprintId.Value),
            (_, true) => issue =>
                issue.SprintId == null || (issue.SprintId != null && ids.Contains(issue.SprintId.Value))
        });
    }

    private static async Task<Result<List<Guid>>> ResolveLabelsAsync(
        IssueFilter filter,
        Guid teamId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();
        var names = new List<string>();

        // Se aceptan identificadores y nombres: la interfaz manda identificadores, y una URL
        // escrita a mano —"label=bug"— es justamente lo que la task pone de ejemplo.
        foreach (var value in filter.Values)
        {
            if (Guid.TryParse(value, out var labelId))
            {
                ids.Add(labelId);
            }
            else
            {
                names.Add(value.ToUpperInvariant());
            }
        }

        if (names.Count > 0)
        {
            var found = await dbContext.Labels
                .AsNoTracking()
                .Where(label => label.TeamId == teamId && names.Contains(label.NormalizedName))
                .Select(label => new { label.Id, label.NormalizedName })
                .ToListAsync(cancellationToken);

            var missing = names.Except(found.Select(label => label.NormalizedName)).ToArray();

            if (missing.Length > 0)
            {
                // Un nombre que no existe se avisa: una vista compartida que en silencio no
                // devuelve nada es más difícil de entender que un error que dice cuál falla.
                return Result.Failure<List<Guid>>(
                    IssueFilterErrors.UnknownValue(filter.Field, missing[0].ToLowerInvariant()));
            }

            ids.AddRange(found.Select(label => label.Id));
        }

        return Result.Success(ids);
    }

    private static async Task<Result<(List<Guid> Ids, bool IncludesNone)>> ParseUsersAsync(
        IssueFilter filter,
        ICurrentUser currentUser,
        bool allowNone,
        CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();
        var includesNone = false;

        foreach (var value in filter.Values)
        {
            if (value.Equals(Me, StringComparison.OrdinalIgnoreCase))
            {
                var userId = await currentUser.RequireIdAsync(cancellationToken);

                if (userId.IsFailure)
                {
                    return Result.Failure<(List<Guid>, bool)>(IssueFilterErrors.NoCurrentUser);
                }

                ids.Add(userId.Value);
            }
            else if (allowNone && value.Equals(None, StringComparison.OrdinalIgnoreCase))
            {
                includesNone = true;
            }
            else if (Guid.TryParse(value, out var parsed))
            {
                ids.Add(parsed);
            }
            else
            {
                return Result.Failure<(List<Guid>, bool)>(
                    IssueFilterErrors.UnknownValue(filter.Field, value));
            }
        }

        return Result.Success((ids, includesNone));
    }

    /// <summary>
    /// Interpreta los valores de un filtro como nombres de un enum.
    /// </summary>
    /// <remarks>
    /// Solo por nombre: <c>Enum.TryParse</c> también acepta números, y admitirlos dejaría
    /// pasar un <c>status=99</c> que no coincide con nada y filtra en silencio.
    /// </remarks>
    private static Result<List<TEnum>> ParseEnums<TEnum>(IssueFilter filter)
        where TEnum : struct, Enum
    {
        var parsed = new List<TEnum>();

        foreach (var value in filter.Values)
        {
            if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var item) ||
                !Enum.IsDefined(item) ||
                char.IsDigit(value[0]))
            {
                return Result.Failure<List<TEnum>>(IssueFilterErrors.UnknownValue(filter.Field, value));
            }

            parsed.Add(item);
        }

        return Result.Success(parsed);
    }

    private static Expression<Func<Issue, bool>> Not(Expression<Func<Issue, bool>> predicate) =>
        Expression.Lambda<Func<Issue, bool>>(Expression.Not(predicate.Body), predicate.Parameters);

    private static string Escape(string value) => value
        .Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
        .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
        .Replace("_", LikeEscape + "_", StringComparison.Ordinal);
}
