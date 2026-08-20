using System.Net;
using System.Net.Http.Json;

using Linear.Domain.Issues;
using Linear.Domain.Roadmaps;
using Linear.Domain.Teams;
using Linear.IntegrationTests.Infrastructure;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Features.Roadmaps.Contracts;
using Linear.Web.Shared.Pagination;

namespace Linear.IntegrationTests.Features.Roadmaps;

[Collection(PostgresCollection.Name)]
public sealed class RoadmapsEndpointTests : IAsyncLifetime
{
    private const string OwnerEmail = "owner@linear.dev";
    private const string MemberEmail = "member@linear.dev";
    private const string OutsiderEmail = "outsider@linear.dev";

    private static readonly DateOnly Start = new(2026, 9, 1);
    private static readonly DateOnly Target = new(2026, 11, 30);

    private readonly PostgresFixture _postgres;
    private readonly DatabaseWebApplicationFactory _factory;

    public RoadmapsEndpointTests(PostgresFixture postgres)
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

    // ---- roadmap -----------------------------------------------------------------------

    [Fact]
    public async Task ANewRoadmapStartsEmpty()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        using var response = await client.PostAsJsonAsync(
            RoadmapsUrl(team),
            new { name = "Segundo semestre", description = "Lo grande del semestre" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var roadmap = await response.Content.ReadFromJsonAsync<RoadmapResponse>();

        Assert.NotNull(roadmap);
        Assert.Equal("Segundo semestre", roadmap.Name);
        Assert.Equal("Lo grande del semestre", roadmap.Description);
        Assert.Empty(roadmap.Items);
    }

    [Fact]
    public async Task ARoadmapWithoutANameIsRejected()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        using var response = await client.PostAsJsonAsync(
            RoadmapsUrl(team), new { name = "   " }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ARoadmapCanBeEdited()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var roadmap = await CreateRoadmapAsync(client, team);

        using var response = await client.PutAsJsonAsync(
            RoadmapUrl(team, roadmap.Id),
            new { name = "Otro nombre", description = "Otra descripción" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<RoadmapResponse>();

        Assert.Equal("Otro nombre", updated!.Name);
        Assert.Equal("Otra descripción", updated.Description);
    }

    [Fact]
    public async Task TheListingCountsTheItemsOfEachRoadmap()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        var withItems = await CreateRoadmapAsync(client, team, "Con iniciativas");
        var empty = await CreateRoadmapAsync(client, team, "Vacío");

        await CreateItemAsync(client, team, withItems.Id, "Autenticación");
        await CreateItemAsync(client, team, withItems.Id, "Dashboard");

        var page = await ListRoadmapsAsync(client, team);

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.Items.Single(roadmap => roadmap.Id == withItems.Id).ItemCount);
        Assert.Equal(0, page.Items.Single(roadmap => roadmap.Id == empty.Id).ItemCount);
    }

    // ---- iniciativas -------------------------------------------------------------------

    [Fact]
    public async Task ANewItemStartsPlanned()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var roadmap = await CreateRoadmapAsync(client, team);

        var updated = await CreateItemAsync(client, team, roadmap.Id, "Autenticación");

        var item = Assert.Single(updated.Items);

        Assert.Equal("Autenticación", item.Name);
        Assert.Equal(nameof(RoadmapItemStatus.Planned), item.Status);
        Assert.Equal(Start, item.StartDate);
        Assert.Equal(Target, item.TargetDate);
        Assert.Equal(RoadmapItemProgress.Empty, item.Progress);
    }

    [Fact]
    public async Task AnItemWhoseTargetIsNotAfterItsStartIsRejected()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var roadmap = await CreateRoadmapAsync(client, team);

        using var response = await client.PostAsJsonAsync(
            ItemsUrl(team, roadmap.Id),
            new { name = "Autenticación", startDate = Target, targetDate = Start },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AnItemCanBeEditedIncludingItsStatus()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var roadmap = await CreateRoadmapAsync(client, team);
        var item = Assert.Single((await CreateItemAsync(client, team, roadmap.Id, "Autenticación")).Items);

        using var response = await client.PutAsJsonAsync(
            ItemUrl(team, roadmap.Id, item.Id),
            new
            {
                name = "SSO",
                description = "Solo SSO",
                startDate = Start,
                targetDate = Target.AddMonths(1),
                status = nameof(RoadmapItemStatus.InProgress)
            },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = Assert.Single((await response.Content.ReadFromJsonAsync<RoadmapResponse>())!.Items);

        Assert.Equal("SSO", updated.Name);
        Assert.Equal(nameof(RoadmapItemStatus.InProgress), updated.Status);
        Assert.Equal(Target.AddMonths(1), updated.TargetDate);
    }

    /// <summary>
    /// El roadmap no define un recorrido obligatorio: una iniciativa completada puede volver
    /// a estar en curso si se reabre.
    /// </summary>
    [Fact]
    public async Task AnItemCanGoBackToAPreviousStatus()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var roadmap = await CreateRoadmapAsync(client, team);
        var item = Assert.Single((await CreateItemAsync(client, team, roadmap.Id, "Autenticación")).Items);

        await UpdateItemStatusAsync(client, team, roadmap.Id, item, RoadmapItemStatus.Completed);
        var reopened = await UpdateItemStatusAsync(
            client, team, roadmap.Id, item, RoadmapItemStatus.InProgress);

        Assert.Equal(nameof(RoadmapItemStatus.InProgress), Assert.Single(reopened.Items).Status);
    }

    [Fact]
    public async Task ItemsComeBackOrderedByStartDate()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var roadmap = await CreateRoadmapAsync(client, team);

        await CreateItemAsync(client, team, roadmap.Id, "Tercera", Start.AddMonths(2), Target.AddMonths(2));
        await CreateItemAsync(client, team, roadmap.Id, "Primera", Start, Target);
        var updated = await CreateItemAsync(
            client, team, roadmap.Id, "Segunda", Start.AddMonths(1), Target.AddMonths(1));

        Assert.Equal(["Primera", "Segunda", "Tercera"], updated.Items.Select(item => item.Name));
    }

    // ---- issues asociados ---------------------------------------------------------------

    [Fact]
    public async Task AnIssueCanBeAssociatedAndRemoved()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var roadmap = await CreateRoadmapAsync(client, team);
        var item = Assert.Single((await CreateItemAsync(client, team, roadmap.Id, "Autenticación")).Items);
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        var withIssue = await PostAsync(client, IssueUrl(team, roadmap.Id, item.Id, issue.Identifier));

        var progress = Assert.Single(withIssue.Items).Progress;
        Assert.Equal(1, progress.TotalIssues);
        Assert.Equal(0, progress.CompletedIssues);

        using var removed = await client.DeleteAsync(
            IssueUrl(team, roadmap.Id, item.Id, issue.Identifier), CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);

        var without = await removed.Content.ReadFromJsonAsync<RoadmapResponse>();
        Assert.Equal(RoadmapItemProgress.Empty, Assert.Single(without!.Items).Progress);
    }

    /// <summary>Un issue aporta a una única iniciativa: asociarlo a otra lo mueve.</summary>
    [Fact]
    public async Task AssociatingAnIssueToAnotherItemMovesIt()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var roadmap = await CreateRoadmapAsync(client, team);

        var created = await CreateItemAsync(client, team, roadmap.Id, "Primera");
        var withBoth = await CreateItemAsync(
            client, team, roadmap.Id, "Segunda", Start.AddMonths(1), Target.AddMonths(1));

        var first = withBoth.Items.Single(item => item.Name == "Primera");
        var second = withBoth.Items.Single(item => item.Name == "Segunda");
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        await PostAsync(client, IssueUrl(team, roadmap.Id, first.Id, issue.Identifier));
        var moved = await PostAsync(client, IssueUrl(team, roadmap.Id, second.Id, issue.Identifier));

        Assert.Equal(0, moved.Items.Single(item => item.Id == first.Id).Progress.TotalIssues);
        Assert.Equal(1, moved.Items.Single(item => item.Id == second.Id).Progress.TotalIssues);
    }

    [Fact]
    public async Task AssociatingTheSameIssueTwiceFails()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var roadmap = await CreateRoadmapAsync(client, team);
        var item = Assert.Single((await CreateItemAsync(client, team, roadmap.Id, "Autenticación")).Items);
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        await PostAsync(client, IssueUrl(team, roadmap.Id, item.Id, issue.Identifier));

        using var response = await client.PostAsJsonAsync(
            IssueUrl(team, roadmap.Id, item.Id, issue.Identifier), new { }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AnIssueFromAnotherTeamCannotBeAssociated()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        var web = await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id, "Web");
        var core = await TeamScenario.CreateTeamAsync(_factory, "CORE", owner.Id, "Core");

        using var client = await AuthenticationScenario.SignInAsync(_factory, OwnerEmail);

        var roadmap = await CreateRoadmapAsync(client, web);
        var item = Assert.Single((await CreateItemAsync(client, web, roadmap.Id, "Autenticación")).Items);
        var foreignIssue = await CreateIssueAsync(client, core, "De otro equipo");

        using var response = await client.PostAsJsonAsync(
            IssueUrl(web, roadmap.Id, item.Id, foreignIssue.Identifier), new { }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RemovingAnIssueThatIsNotAssociatedFails()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var roadmap = await CreateRoadmapAsync(client, team);
        var item = Assert.Single((await CreateItemAsync(client, team, roadmap.Id, "Autenticación")).Items);
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        using var response = await client.DeleteAsync(
            IssueUrl(team, roadmap.Id, item.Id, issue.Identifier), CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>El avance cuenta los issues asociados, y los terminados aparte.</summary>
    [Fact]
    public async Task TheProgressCountsCompletedIssues()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var roadmap = await CreateRoadmapAsync(client, team);
        var item = Assert.Single((await CreateItemAsync(client, team, roadmap.Id, "Autenticación")).Items);

        var done = await CreateIssueAsync(client, team, "Ya está");
        var pending = await CreateIssueAsync(client, team, "Pendiente");
        var alsoPending = await CreateIssueAsync(client, team, "También pendiente");
        var stillPending = await CreateIssueAsync(client, team, "Y otro más");

        foreach (var issue in new[] { done, pending, alsoPending, stillPending })
        {
            await PostAsync(client, IssueUrl(team, roadmap.Id, item.Id, issue.Identifier));
        }

        await ChangeStatusAsync(client, team, done.Identifier, IssueStatus.Done);

        var progress = Assert.Single((await GetRoadmapAsync(client, team, roadmap.Id)).Items).Progress;

        Assert.Equal(4, progress.TotalIssues);
        Assert.Equal(1, progress.CompletedIssues);
        Assert.Equal(25, progress.CompletionPercentage);
    }

    [Fact]
    public async Task TheIssueDetailShowsItsRoadmapItem()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var roadmap = await CreateRoadmapAsync(client, team, "Segundo semestre");
        var item = Assert.Single((await CreateItemAsync(client, team, roadmap.Id, "Autenticación")).Items);
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        Assert.Null(issue.RoadmapItem);

        await PostAsync(client, IssueUrl(team, roadmap.Id, item.Id, issue.Identifier));

        using var response = await client.GetAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}", CancellationToken.None);

        var refreshed = await response.Content.ReadFromJsonAsync<IssueResponse>();

        Assert.Equal(item.Id, refreshed!.RoadmapItem!.Id);
        Assert.Equal("Autenticación", refreshed.RoadmapItem.Name);
        Assert.Equal(roadmap.Id, refreshed.RoadmapItem.RoadmapId);
        Assert.Equal("Segundo semestre", refreshed.RoadmapItem.RoadmapName);
    }

    /// <summary>El filtro del listado de issues también entiende la iniciativa.</summary>
    [Fact]
    public async Task IssuesCanBeFilteredByRoadmapItem()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var roadmap = await CreateRoadmapAsync(client, team);
        var item = Assert.Single((await CreateItemAsync(client, team, roadmap.Id, "Autenticación")).Items);

        var associated = await CreateIssueAsync(client, team, "Arreglar el login");
        await CreateIssueAsync(client, team, "Sin iniciativa");

        await PostAsync(client, IssueUrl(team, roadmap.Id, item.Id, associated.Identifier));

        Assert.Equal([associated.Identifier], await FilterIssuesAsync(client, team, $"roadmapItem={item.Id}"));

        var without = await FilterIssuesAsync(client, team, "roadmapItem=none");
        Assert.DoesNotContain(associated.Identifier, without);
    }

    // ---- eliminación --------------------------------------------------------------------

    /// <summary>
    /// Eliminar es definitivo, así que pide rol Admin — igual que eliminar un issue, una
    /// label o el equipo mismo.
    /// </summary>
    [Fact]
    public async Task AMemberCannotDeleteARoadmap()
    {
        var team = await ATeamAsync(withMember: true);
        using var ownerClient = await SignInAsync(OwnerEmail);
        var roadmap = await CreateRoadmapAsync(ownerClient, team);

        using var memberClient = await SignInAsync(MemberEmail);

        using var response = await memberClient.DeleteAsync(
            RoadmapUrl(team, roadmap.Id), CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AMemberCanPlanButNotDeleteAnItem()
    {
        var team = await ATeamAsync(withMember: true);
        using var ownerClient = await SignInAsync(OwnerEmail);
        var roadmap = await CreateRoadmapAsync(ownerClient, team);

        using var memberClient = await SignInAsync(MemberEmail);

        // Planificar sí: crear la iniciativa es trabajo del día a día.
        var created = await CreateItemAsync(memberClient, team, roadmap.Id, "Autenticación");
        var item = Assert.Single(created.Items);

        using var response = await memberClient.DeleteAsync(
            ItemUrl(team, roadmap.Id, item.Id), CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeletingARoadmapLeavesItsIssuesStanding()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var roadmap = await CreateRoadmapAsync(client, team);
        var item = Assert.Single((await CreateItemAsync(client, team, roadmap.Id, "Autenticación")).Items);
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        await PostAsync(client, IssueUrl(team, roadmap.Id, item.Id, issue.Identifier));

        using var deleted = await client.DeleteAsync(RoadmapUrl(team, roadmap.Id), CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        // El issue sobrevive, sin iniciativa: el trabajo no se va con el plan que lo agrupaba.
        using var response = await client.GetAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var refreshed = await response.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.Null(refreshed!.RoadmapItem);
    }

    [Fact]
    public async Task DeletingAnItemLeavesItsIssuesStanding()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var roadmap = await CreateRoadmapAsync(client, team);
        var item = Assert.Single((await CreateItemAsync(client, team, roadmap.Id, "Autenticación")).Items);
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        await PostAsync(client, IssueUrl(team, roadmap.Id, item.Id, issue.Identifier));

        using var deleted = await client.DeleteAsync(
            ItemUrl(team, roadmap.Id, item.Id), CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        Assert.Empty((await GetRoadmapAsync(client, team, roadmap.Id)).Items);

        using var response = await client.GetAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}", CancellationToken.None);

        var refreshed = await response.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.Null(refreshed!.RoadmapItem);
    }

    [Fact]
    public async Task DeletingATeamRemovesItsRoadmaps()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var roadmap = await CreateRoadmapAsync(client, team);
        await CreateItemAsync(client, team, roadmap.Id, "Autenticación");

        using var deleted = await client.DeleteAsync($"/api/teams/{team.Key.Value}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<Web.Infrastructure.Persistence.AppDbContext>();

        Assert.Empty(dbContext.Roadmaps);
    }

    // ---- aislamiento ---------------------------------------------------------------------

    [Fact]
    public async Task SomeoneOutsideTheTeamSeesNoRoadmaps()
    {
        var team = await ATeamAsync();
        using var ownerClient = await SignInAsync(OwnerEmail);
        var roadmap = await CreateRoadmapAsync(ownerClient, team);

        await AuthenticationScenario.CreateUserAsync(_factory, OutsiderEmail);
        using var outsiderClient = await AuthenticationScenario.SignInAsync(_factory, OutsiderEmail);

        using var list = await outsiderClient.GetAsync(RoadmapsUrl(team), CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);

        using var detail = await outsiderClient.GetAsync(RoadmapUrl(team, roadmap.Id), CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
    }

    [Fact]
    public async Task ARoadmapIsNotReachableThroughAnotherTeam()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        var web = await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id, "Web");
        var core = await TeamScenario.CreateTeamAsync(_factory, "CORE", owner.Id, "Core");

        using var client = await AuthenticationScenario.SignInAsync(_factory, OwnerEmail);
        var roadmap = await CreateRoadmapAsync(client, web);

        using var response = await client.GetAsync(RoadmapUrl(core, roadmap.Id), CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WithoutASessionTheRoadmapsApiRespondsUnauthorized()
    {
        using var client = AuthenticationScenario.CreateClient(_factory);

        using var response = await client.GetAsync("/api/teams/WEB/roadmaps", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- escenario ------------------------------------------------------------------------

    private static string RoadmapsUrl(Team team) => $"/api/teams/{team.Key.Value}/roadmaps";

    private static string RoadmapUrl(Team team, Guid roadmapId) => $"{RoadmapsUrl(team)}/{roadmapId}";

    private static string ItemsUrl(Team team, Guid roadmapId) => $"{RoadmapUrl(team, roadmapId)}/items";

    private static string ItemUrl(Team team, Guid roadmapId, Guid itemId) =>
        $"{ItemsUrl(team, roadmapId)}/{itemId}";

    private static string IssueUrl(Team team, Guid roadmapId, Guid itemId, string identifier) =>
        $"{ItemUrl(team, roadmapId, itemId)}/issues/{identifier}";

    private async Task<Team> ATeamAsync(bool withMember = false)
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        var team = await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id, "Web");

        if (withMember)
        {
            var member = await AuthenticationScenario.CreateUserAsync(_factory, MemberEmail);
            await TeamScenario.AddMemberAsync(_factory, team.Id, member.Id, TeamRole.Member);
        }

        return team;
    }

    private Task<HttpClient> SignInAsync(string email) =>
        AuthenticationScenario.SignInAsync(_factory, email);

    private static async Task<RoadmapResponse> CreateRoadmapAsync(
        HttpClient client,
        Team team,
        string name = "Segundo semestre")
    {
        using var response = await client.PostAsJsonAsync(
            RoadmapsUrl(team), new { name }, CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<RoadmapResponse>())!;
    }

    private static async Task<RoadmapResponse> CreateItemAsync(
        HttpClient client,
        Team team,
        Guid roadmapId,
        string name,
        DateOnly? startDate = null,
        DateOnly? targetDate = null)
    {
        using var response = await client.PostAsJsonAsync(
            ItemsUrl(team, roadmapId),
            new { name, startDate = startDate ?? Start, targetDate = targetDate ?? Target },
            CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<RoadmapResponse>())!;
    }

    private static async Task<RoadmapResponse> UpdateItemStatusAsync(
        HttpClient client,
        Team team,
        Guid roadmapId,
        RoadmapItemResponse item,
        RoadmapItemStatus status)
    {
        using var response = await client.PutAsJsonAsync(
            ItemUrl(team, roadmapId, item.Id),
            new
            {
                name = item.Name,
                description = item.Description,
                startDate = item.StartDate,
                targetDate = item.TargetDate,
                status = status.ToString()
            },
            CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<RoadmapResponse>())!;
    }

    private static async Task<RoadmapResponse> GetRoadmapAsync(HttpClient client, Team team, Guid roadmapId)
    {
        using var response = await client.GetAsync(RoadmapUrl(team, roadmapId), CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<RoadmapResponse>())!;
    }

    private static async Task<PagedResult<RoadmapSummaryResponse>> ListRoadmapsAsync(
        HttpClient client,
        Team team)
    {
        using var response = await client.GetAsync(RoadmapsUrl(team), CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<PagedResult<RoadmapSummaryResponse>>())!;
    }

    private static async Task<RoadmapResponse> PostAsync(HttpClient client, string url)
    {
        using var response = await client.PostAsJsonAsync(url, new { }, CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<RoadmapResponse>())!;
    }

    private static async Task<IssueResponse> CreateIssueAsync(HttpClient client, Team team, string title)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues", new { title }, CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<IssueResponse>())!;
    }

    private static async Task ChangeStatusAsync(
        HttpClient client,
        Team team,
        string identifier,
        IssueStatus status)
    {
        using var response = await client.PutAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{identifier}/status",
            new { status = status.ToString() },
            CancellationToken.None);

        response.EnsureSuccessStatusCode();
    }

    private static async Task<string[]> FilterIssuesAsync(HttpClient client, Team team, string query)
    {
        using var response = await client.GetAsync(
            $"/api/teams/{team.Key.Value}/issues?{query}", CancellationToken.None);

        response.EnsureSuccessStatusCode();

        var page = await response.Content.ReadFromJsonAsync<PagedResult<IssueSummaryResponse>>();

        return [.. page!.Items.Select(issue => issue.Identifier)];
    }
}
