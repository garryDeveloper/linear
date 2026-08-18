using Linear.Domain.Teams;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.IntegrationTests.Infrastructure;

/// <summary>
/// Crea equipos directamente en la base, para preparar escenarios sin depender de que
/// el endpoint de creación funcione.
/// </summary>
internal static class TeamScenario
{
    public static async Task<Team> CreateTeamAsync(
        DatabaseWebApplicationFactory factory,
        string key,
        Guid ownerUserId,
        string name = "Equipo de prueba")
    {
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var team = Team.Create(
            name,
            TeamKey.Create(key).Value,
            description: null,
            ownerUserId,
            DateTimeOffset.UtcNow).Value;

        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        return team;
    }

    public static async Task AddMemberAsync(
        DatabaseWebApplicationFactory factory,
        Guid teamId,
        Guid userId,
        TeamRole role)
    {
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var team = await dbContext.Teams
            .Include(team => team.Members)
            .FirstAsync(team => team.Id == teamId);

        team.AddMember(userId, role, DateTimeOffset.UtcNow);

        await dbContext.SaveChangesAsync();
    }
}
