using Linear.Domain.Common;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Persistence;
using Linear.Web.Shared.Pagination;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Teams.List;

/// <summary>
/// Lista los equipos a los que pertenece el usuario en curso.
/// </summary>
/// <remarks>
/// No necesita comprobar permisos: el filtro por pertenencia es la propia autorización.
/// Un usuario nunca ve un equipo del que no es miembro.
/// </remarks>
public sealed class ListTeamsHandler(AppDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<Result<PagedResult<TeamSummaryResponse>>> HandleAsync(
        ListTeamsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = await currentUser.RequireIdAsync(cancellationToken);

        if (userId.IsFailure)
        {
            return Result.Failure<PagedResult<TeamSummaryResponse>>(userId.Error);
        }

        var page = request.ToPageRequest();

        var query = dbContext.Teams
            .AsNoTracking()
            .Where(team => team.Members.Any(member => member.UserId == userId.Value));

        var totalCount = await query.CountAsync(cancellationToken);

        // Se proyecta la clave completa y no su contenido: TeamKey se persiste con un
        // conversor, y leer dentro del value object no es traducible a SQL.
        var rows = await query
            .OrderBy(team => team.Name)
            .Skip(page.Skip)
            .Take(page.Take)
            .Select(team => new
            {
                team.Id,
                team.Key,
                team.Name,
                team.Description,
                MemberCount = team.Members.Count,
                Role = team.Members
                    .Where(member => member.UserId == userId.Value)
                    .Select(member => member.Role)
                    .FirstOrDefault()
            })
            .ToArrayAsync(cancellationToken);

        var items = rows
            .Select(row => new TeamSummaryResponse(
                row.Id,
                row.Key.Value,
                row.Name,
                row.Description,
                row.Role.ToString(),
                row.MemberCount))
            .ToArray();

        return Result.Success(PagedResult<TeamSummaryResponse>.Create(items, page, totalCount));
    }
}
