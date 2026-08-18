using System.Net;
using System.Net.Http.Json;

using Linear.Domain.Teams;
using Linear.Domain.Users;
using Linear.IntegrationTests.Infrastructure;
using Linear.Web.Features.Teams.Contracts;
using Linear.Web.Infrastructure.Persistence;
using Linear.Web.Shared.Pagination;
using Linear.Web.Shared.Results;

using Microsoft.EntityFrameworkCore;

namespace Linear.IntegrationTests.Features.Teams;

[Collection(PostgresCollection.Name)]
public sealed class TeamsEndpointTests : IAsyncLifetime
{
    private const string OwnerEmail = "owner@linear.dev";
    private const string AdminEmail = "admin@linear.dev";
    private const string MemberEmail = "member@linear.dev";
    private const string OutsiderEmail = "outsider@linear.dev";

    private readonly PostgresFixture _postgres;
    private readonly DatabaseWebApplicationFactory _factory;

    public TeamsEndpointTests(PostgresFixture postgres)
    {
        _postgres = postgres;
        _factory = new DatabaseWebApplicationFactory(postgres.ConnectionString);
    }

    public Task InitializeAsync() => _postgres.ResetAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreatingATeamLeavesTheCreatorAsOwner()
    {
        using var client = await SignInAsAsync(OwnerEmail);

        using var response = await client.PostAsJsonAsync(
            "/api/teams",
            new { name = "Web", key = "web", description = "El equipo de la web" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var team = await response.Content.ReadFromJsonAsync<TeamResponse>();

        Assert.NotNull(team);
        Assert.Equal("WEB", team.Key);
        Assert.Equal(nameof(TeamRole.Owner), team.Role);

        var member = Assert.Single(team.Members);
        Assert.Equal(OwnerEmail, member.Email);
        Assert.Equal(nameof(TeamRole.Owner), member.Role);
    }

    [Fact]
    public async Task TheTeamKeyIsUnique()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id);

        using var client = await SignInAsAsync(OwnerEmail, createUser: false);

        using var response = await client.PostAsJsonAsync(
            "/api/teams",
            new { name = "Otro equipo", key = "WEB" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(TeamErrors.KeyAlreadyExists.Code, error!.Code);
    }

    [Fact]
    public async Task AMalformedKeyIsRejected()
    {
        using var client = await SignInAsAsync(OwnerEmail);

        using var response = await client.PostAsJsonAsync(
            "/api/teams",
            new { name = "Web", key = "1WEB" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TheListingOnlyShowsTheTeamsOfTheUser()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        var outsider = await AuthenticationScenario.CreateUserAsync(_factory, OutsiderEmail);

        await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id, "Web");
        await TeamScenario.CreateTeamAsync(_factory, "CORE", owner.Id, "Core");
        await TeamScenario.CreateTeamAsync(_factory, "OTHER", outsider.Id, "Ajeno");

        using var client = await SignInAsAsync(OwnerEmail, createUser: false);

        var teams = await client.GetFromJsonAsync<PagedResult<TeamSummaryResponse>>(
            "/api/teams",
            CancellationToken.None);

        Assert.NotNull(teams);
        Assert.Equal(2, teams.TotalCount);
        Assert.DoesNotContain(teams.Items, team => team.Key == "OTHER");
    }

    [Fact]
    public async Task TheListingIsPaginated()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);

        await TeamScenario.CreateTeamAsync(_factory, "AAA", owner.Id, "Alfa");
        await TeamScenario.CreateTeamAsync(_factory, "BBB", owner.Id, "Beta");
        await TeamScenario.CreateTeamAsync(_factory, "CCC", owner.Id, "Gama");

        using var client = await SignInAsAsync(OwnerEmail, createUser: false);

        var page = await client.GetFromJsonAsync<PagedResult<TeamSummaryResponse>>(
            "/api/teams?page=2&pageSize=2",
            CancellationToken.None);

        Assert.NotNull(page);
        Assert.Equal(3, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal("Gama", page.Items[0].Name);
        Assert.False(page.HasNextPage);
    }

    [Fact]
    public async Task AnOutsiderDoesNotSeeTheTeam()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id);

        using var client = await SignInAsAsync(OutsiderEmail);

        using var response = await client.GetAsync("/api/teams/WEB", CancellationToken.None);

        // No se distingue de un equipo inexistente: responder 403 confirmaria que la
        // clave WEB corresponde a un equipo real.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AMemberCannotEditTheTeam()
    {
        var team = await ATeamWithTheThreeRolesAsync();
        using var client = await SignInAsAsync(MemberEmail, createUser: false);

        using var response = await client.PutAsJsonAsync(
            $"/api/teams/{team.Key.Value}",
            new { name = "Renombrado" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AnAdminCanEditTheTeam()
    {
        var team = await ATeamWithTheThreeRolesAsync();
        using var client = await SignInAsAsync(AdminEmail, createUser: false);

        using var response = await client.PutAsJsonAsync(
            $"/api/teams/{team.Key.Value}",
            new { name = "Renombrado", description = "Nueva descripcion" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<TeamResponse>();
        Assert.Equal("Renombrado", updated!.Name);
    }

    [Fact]
    public async Task OnlyTheOwnerCanDeleteTheTeam()
    {
        var team = await ATeamWithTheThreeRolesAsync();

        using var adminClient = await SignInAsAsync(AdminEmail, createUser: false);
        using var deniedForAdmin = await adminClient.DeleteAsync(
            $"/api/teams/{team.Key.Value}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, deniedForAdmin.StatusCode);

        using var ownerClient = await SignInAsAsync(OwnerEmail, createUser: false);
        using var deleted = await ownerClient.DeleteAsync(
            $"/api/teams/{team.Key.Value}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var afterDeletion = await ownerClient.GetAsync(
            $"/api/teams/{team.Key.Value}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, afterDeletion.StatusCode);
    }

    [Fact]
    public async Task AnAdminCanAddAMember()
    {
        var team = await ATeamWithTheThreeRolesAsync();
        await AuthenticationScenario.CreateUserAsync(_factory, OutsiderEmail);

        using var client = await SignInAsAsync(AdminEmail, createUser: false);

        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/members",
            new { email = OutsiderEmail, role = nameof(TeamRole.Member) },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<TeamResponse>();
        Assert.Contains(updated!.Members, member => member.Email == OutsiderEmail);
    }

    [Fact]
    public async Task AddingSomeoneWithoutAnAccountFails()
    {
        var team = await ATeamWithTheThreeRolesAsync();
        using var client = await SignInAsAsync(AdminEmail, createUser: false);

        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/members",
            new { email = "nadie@linear.dev", role = nameof(TeamRole.Member) },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AnAdminCannotGrantTheOwnerRole()
    {
        var team = await ATeamWithTheThreeRolesAsync();
        var memberId = await UserIdOfAsync(MemberEmail);

        using var client = await SignInAsAsync(AdminEmail, createUser: false);

        using var response = await client.PutAsJsonAsync(
            $"/api/teams/{team.Key.Value}/members/{memberId}/role",
            new { role = nameof(TeamRole.Owner) },
            CancellationToken.None);

        // De poder hacerlo, un Admin se concederia el rol Owner a traves de un tercero.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(TeamErrors.OnlyOwnersManageOwners.Code, error!.Code);
    }

    [Fact]
    public async Task AnAdminCannotRemoveAnOwner()
    {
        var team = await ATeamWithTheThreeRolesAsync();
        var ownerId = await UserIdOfAsync(OwnerEmail);

        using var client = await SignInAsAsync(AdminEmail, createUser: false);

        using var response = await client.DeleteAsync(
            $"/api/teams/{team.Key.Value}/members/{ownerId}",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TheOwnerCanPromoteAMember()
    {
        var team = await ATeamWithTheThreeRolesAsync();
        var memberId = await UserIdOfAsync(MemberEmail);

        using var client = await SignInAsAsync(OwnerEmail, createUser: false);

        using var response = await client.PutAsJsonAsync(
            $"/api/teams/{team.Key.Value}/members/{memberId}/role",
            new { role = nameof(TeamRole.Admin) },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<TeamResponse>();
        var promoted = Assert.Single(updated!.Members, member => member.Email == MemberEmail);
        Assert.Equal(nameof(TeamRole.Admin), promoted.Role);
    }

    [Fact]
    public async Task TheLastOwnerCannotBeDemoted()
    {
        var team = await ATeamWithTheThreeRolesAsync();
        var ownerId = await UserIdOfAsync(OwnerEmail);

        using var client = await SignInAsAsync(OwnerEmail, createUser: false);

        using var response = await client.PutAsJsonAsync(
            $"/api/teams/{team.Key.Value}/members/{ownerId}/role",
            new { role = nameof(TeamRole.Admin) },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(TeamErrors.LastOwner.Code, error!.Code);
    }

    [Fact]
    public async Task TheSameUserCannotBeAddedTwice()
    {
        var team = await ATeamWithTheThreeRolesAsync();
        using var client = await SignInAsAsync(OwnerEmail, createUser: false);

        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/members",
            new { email = MemberEmail, role = nameof(TeamRole.Member) },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task WithoutASessionTheTeamsApiRespondsUnauthorized()
    {
        using var client = AuthenticationScenario.CreateClient(_factory);

        using var response = await client.GetAsync("/api/teams", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<Team> ATeamWithTheThreeRolesAsync()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        var admin = await AuthenticationScenario.CreateUserAsync(_factory, AdminEmail);
        var member = await AuthenticationScenario.CreateUserAsync(_factory, MemberEmail);

        var team = await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id, "Web");

        await TeamScenario.AddMemberAsync(_factory, team.Id, admin.Id, TeamRole.Admin);
        await TeamScenario.AddMemberAsync(_factory, team.Id, member.Id, TeamRole.Member);

        return team;
    }

    private async Task<HttpClient> SignInAsAsync(string email, bool createUser = true)
    {
        if (createUser)
        {
            await AuthenticationScenario.CreateUserAsync(_factory, email);
        }

        return await AuthenticationScenario.SignInAsync(_factory, email);
    }

    private async Task<Guid> UserIdOfAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var target = Email.Create(email).Value;

        var user = await dbContext.Users.FirstAsync(candidate => candidate.Email == target);

        return user.Id;
    }
}
