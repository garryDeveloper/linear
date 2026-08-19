using Linear.Domain.Common;
using Linear.Domain.Teams;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Authorization;
using Linear.Web.Infrastructure.Persistence;

namespace Linear.Web.Features.Labels.Contracts;

/// <summary>
/// Resuelve el equipo de la ruta y comprueba el permiso necesario para operar sus labels.
/// </summary>
/// <remarks>
/// Leer labels alcanza con pertenecer al equipo; crearlas, editarlas y borrarlas es
/// administrar la configuración del equipo, así que pide rol Admin u Owner. Los cuatro
/// slices comparten este paso, que además garantiza el aislamiento entre equipos: nunca se
/// consulta una label sin haber verificado antes el acceso a su equipo.
/// </remarks>
public static class TeamLabelAccess
{
    public static async Task<Result<Team>> ForReadingAsync(
        ITeamAccess teamAccess,
        AppDbContext dbContext,
        string? teamKey,
        CancellationToken cancellationToken) =>
        await ResolveAsync(teamAccess, dbContext, teamKey, TeamRole.Member, cancellationToken);

    public static async Task<Result<Team>> ForManagingAsync(
        ITeamAccess teamAccess,
        AppDbContext dbContext,
        string? teamKey,
        CancellationToken cancellationToken) =>
        await ResolveAsync(teamAccess, dbContext, teamKey, TeamRole.Admin, cancellationToken);

    private static async Task<Result<Team>> ResolveAsync(
        ITeamAccess teamAccess,
        AppDbContext dbContext,
        string? teamKey,
        TeamRole minimumRole,
        CancellationToken cancellationToken)
    {
        var key = TeamKeyRoute.Parse(teamKey);

        if (key.IsFailure)
        {
            return Result.Failure<Team>(key.Error);
        }

        return await teamAccess.RequireRoleAsync(
            dbContext,
            key.Value,
            minimumRole,
            tracking: false,
            cancellationToken);
    }
}
