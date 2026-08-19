using System.Net;
using System.Net.Http.Json;

using Linear.Domain.Issues;
using Linear.Domain.Sprints;
using Linear.Domain.Teams;
using Linear.IntegrationTests.Infrastructure;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Infrastructure.Persistence;
using Linear.Web.Shared.Pagination;

using Microsoft.EntityFrameworkCore;

namespace Linear.IntegrationTests.Features.Sprints;

[Collection(PostgresCollection.Name)]
public sealed class SprintsEndpointTests : IAsyncLifetime
{
    private const string OwnerEmail = "owner@linear.dev";
    private const string MemberEmail = "member@linear.dev";
    private const string OutsiderEmail = "outsider@linear.dev";

    private static readonly DateOnly Start = new(2026, 8, 19);
    private static readonly DateOnly End = new(2026, 9, 2);

    private readonly PostgresFixture _postgres;
    private readonly DatabaseWebApplicationFactory _factory;

    public SprintsEndpointTests(PostgresFixture postgres)
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
    public async Task ANewSprintStartsPlanned()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        using var response = await client.PostAsJsonAsync(
            SprintsUrl(team),
            new { name = "Sprint 12", goal = "Cerrar el checkout", startDate = Start, endDate = End },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sprint = await response.Content.ReadFromJsonAsync<SprintResponse>();

        Assert.NotNull(sprint);
        Assert.Equal("Sprint 12", sprint.Name);
        Assert.Equal("Cerrar el checkout", sprint.Goal);
        Assert.Equal(nameof(SprintStatus.Planned), sprint.Status);
        Assert.Equal(Start, sprint.StartDate);
        Assert.Equal(End, sprint.EndDate);
        Assert.Empty(sprint.Issues);
        Assert.Equal(SprintMetrics.Empty, sprint.Metrics);
    }

    [Fact]
    public async Task ASprintWhoseEndDateIsNotAfterItsStartDateIsRejected()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        using var response = await client.PostAsJsonAsync(
            SprintsUrl(team),
            new { name = "Sprint 12", startDate = End, endDate = Start },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ASprintWithoutANameIsRejected()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        using var response = await client.PostAsJsonAsync(
            SprintsUrl(team),
            new { name = "  ", startDate = Start, endDate = End },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ASprintCanBeStartedAndCompleted()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var sprint = await CreateSprintAsync(client, team, "Sprint 12");

        var started = await PostAsync(client, $"{SprintUrl(team, sprint.Id)}/start");
        Assert.Equal(nameof(SprintStatus.Active), started.Status);
        Assert.Null(started.CompletedAt);

        var completed = await PostAsync(client, $"{SprintUrl(team, sprint.Id)}/complete");
        Assert.Equal(nameof(SprintStatus.Completed), completed.Status);
        Assert.NotNull(completed.CompletedAt);
    }

    [Fact]
    public async Task ASprintCannotBeCompletedBeforeBeingStarted()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var sprint = await CreateSprintAsync(client, team, "Sprint 12");

        using var response = await client.PostAsJsonAsync(
            $"{SprintUrl(team, sprint.Id)}/complete", new { }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>La regla central de la task: un solo sprint activo por equipo.</summary>
    [Fact]
    public async Task ATeamCannotHaveTwoActiveSprints()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        var first = await CreateSprintAsync(client, team, "Sprint 12");
        var second = await CreateSprintAsync(client, team, "Sprint 13");

        await PostAsync(client, $"{SprintUrl(team, first.Id)}/start");

        using var response = await client.PostAsJsonAsync(
            $"{SprintUrl(team, second.Id)}/start", new { }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>
    /// El chequeo previo del handler tiene una ventana de carrera; quien realmente sostiene
    /// la regla es el índice único parcial. Con varios pedidos simultáneos, exactamente uno
    /// tiene que quedar activo.
    /// </summary>
    [Fact]
    public async Task ConcurrentStartsLeaveExactlyOneActiveSprint()
    {
        var team = await ATeamAsync();
        using var setupClient = await SignInAsync(OwnerEmail);

        const int concurrentRequests = 8;

        var sprints = new List<SprintResponse>();

        for (var index = 1; index <= concurrentRequests; index++)
        {
            sprints.Add(await CreateSprintAsync(setupClient, team, $"Sprint {index}"));
        }

        var statuses = await Task.WhenAll(sprints.Select(async sprint =>
        {
            using var client = await AuthenticationScenario.SignInAsync(_factory, OwnerEmail);
            using var response = await client.PostAsJsonAsync(
                $"{SprintUrl(team, sprint.Id)}/start", new { }, CancellationToken.None);
            return response.StatusCode;
        }));

        Assert.Equal(1, statuses.Count(status => status == HttpStatusCode.OK));
        Assert.Equal(concurrentRequests - 1, statuses.Count(status => status == HttpStatusCode.Conflict));

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(1, await dbContext.Sprints.CountAsync(s => s.Status == SprintStatus.Active));
    }

    [Fact]
    public async Task CancelingTheActiveSprintFreesTheTeamToStartAnother()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        var first = await CreateSprintAsync(client, team, "Sprint 12");
        var second = await CreateSprintAsync(client, team, "Sprint 13");

        await PostAsync(client, $"{SprintUrl(team, first.Id)}/start");

        var canceled = await PostAsync(client, $"{SprintUrl(team, first.Id)}/cancel");
        Assert.Equal(nameof(SprintStatus.Canceled), canceled.Status);
        Assert.Null(canceled.CompletedAt);

        var started = await PostAsync(client, $"{SprintUrl(team, second.Id)}/start");
        Assert.Equal(nameof(SprintStatus.Active), started.Status);
    }

    [Fact]
    public async Task CompletingTheActiveSprintFreesTheTeamToStartAnother()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        var first = await CreateSprintAsync(client, team, "Sprint 12");
        var second = await CreateSprintAsync(client, team, "Sprint 13");

        await PostAsync(client, $"{SprintUrl(team, first.Id)}/start");
        await PostAsync(client, $"{SprintUrl(team, first.Id)}/complete");

        var started = await PostAsync(client, $"{SprintUrl(team, second.Id)}/start");
        Assert.Equal(nameof(SprintStatus.Active), started.Status);
    }

    /// <summary>Cada equipo tiene su propio cupo de sprint activo.</summary>
    [Fact]
    public async Task TwoTeamsCanEachHaveTheirOwnActiveSprint()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        var web = await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id);
        var core = await TeamScenario.CreateTeamAsync(_factory, "CORE", owner.Id);

        using var client = await AuthenticationScenario.SignInAsync(_factory, OwnerEmail);

        var webSprint = await CreateSprintAsync(client, web, "Sprint Web");
        var coreSprint = await CreateSprintAsync(client, core, "Sprint Core");

        var startedWeb = await PostAsync(client, $"{SprintUrl(web, webSprint.Id)}/start");
        var startedCore = await PostAsync(client, $"{SprintUrl(core, coreSprint.Id)}/start");

        Assert.Equal(nameof(SprintStatus.Active), startedWeb.Status);
        Assert.Equal(nameof(SprintStatus.Active), startedCore.Status);
    }

    [Fact]
    public async Task AClosedSprintCannotBeEdited()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var sprint = await CreateSprintAsync(client, team, "Sprint 12");

        await PostAsync(client, $"{SprintUrl(team, sprint.Id)}/cancel");

        using var response = await client.PutAsJsonAsync(
            SprintUrl(team, sprint.Id),
            new { name = "Otro nombre", startDate = Start, endDate = End },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AnIssueCanBeAddedToASprintAndRemoved()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        var sprint = await CreateSprintAsync(client, team, "Sprint 12");
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        var withIssue = await PostAsync(client, IssueInSprintUrl(team, sprint.Id, issue.Identifier));

        Assert.Equal(issue.Identifier, Assert.Single(withIssue.Issues).Identifier);
        Assert.Equal(1, withIssue.Metrics.Total);
        Assert.Equal(1, withIssue.Metrics.Remaining);

        using var removed = await client.DeleteAsync(
            IssueInSprintUrl(team, sprint.Id, issue.Identifier), CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);

        var withoutIssue = await removed.Content.ReadFromJsonAsync<SprintResponse>();
        Assert.Empty(withoutIssue!.Issues);
    }

    /// <summary>"Un Issue puede pertenecer a un único Sprint": sumarlo a otro lo mueve.</summary>
    [Fact]
    public async Task AddingAnIssueToAnotherSprintMovesIt()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        var first = await CreateSprintAsync(client, team, "Sprint 12");
        var second = await CreateSprintAsync(client, team, "Sprint 13");
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        await PostAsync(client, IssueInSprintUrl(team, first.Id, issue.Identifier));
        var moved = await PostAsync(client, IssueInSprintUrl(team, second.Id, issue.Identifier));

        Assert.Equal(issue.Identifier, Assert.Single(moved.Issues).Identifier);

        var origin = await GetSprintAsync(client, team, first.Id);
        Assert.Empty(origin.Issues);
    }

    [Fact]
    public async Task AddingTheSameIssueTwiceFails()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        var sprint = await CreateSprintAsync(client, team, "Sprint 12");
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        await PostAsync(client, IssueInSprintUrl(team, sprint.Id, issue.Identifier));

        using var response = await client.PostAsJsonAsync(
            IssueInSprintUrl(team, sprint.Id, issue.Identifier), new { }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AnIssueFromAnotherTeamCannotBeAddedToTheSprint()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        var web = await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id);
        var core = await TeamScenario.CreateTeamAsync(_factory, "CORE", owner.Id);

        using var client = await AuthenticationScenario.SignInAsync(_factory, OwnerEmail);

        var sprint = await CreateSprintAsync(client, web, "Sprint Web");
        var foreignIssue = await CreateIssueAsync(client, core, "De otro equipo");

        using var response = await client.PostAsJsonAsync(
            IssueInSprintUrl(web, sprint.Id, foreignIssue.Identifier), new { }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task IssuesCannotBeAddedToAClosedSprint()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        var sprint = await CreateSprintAsync(client, team, "Sprint 12");
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        await PostAsync(client, $"{SprintUrl(team, sprint.Id)}/cancel");

        using var response = await client.PostAsJsonAsync(
            IssueInSprintUrl(team, sprint.Id, issue.Identifier), new { }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RemovingAnIssueThatIsNotInTheSprintFails()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        var sprint = await CreateSprintAsync(client, team, "Sprint 12");
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        using var response = await client.DeleteAsync(
            IssueInSprintUrl(team, sprint.Id, issue.Identifier), CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Las métricas que pide la task, contadas sobre datos reales.</summary>
    [Fact]
    public async Task TheMetricsCountCompletedAndRemainingIssues()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        var sprint = await CreateSprintAsync(client, team, "Sprint 12");

        var done = await CreateIssueAsync(client, team, "Ya está");
        var inProgress = await CreateIssueAsync(client, team, "En curso");
        var todo = await CreateIssueAsync(client, team, "Pendiente");
        var alsoTodo = await CreateIssueAsync(client, team, "También pendiente");

        foreach (var issue in new[] { done, inProgress, todo, alsoTodo })
        {
            await PostAsync(client, IssueInSprintUrl(team, sprint.Id, issue.Identifier));
        }

        await ChangeStatusAsync(client, team, done.Identifier, IssueStatus.Done);
        await ChangeStatusAsync(client, team, inProgress.Identifier, IssueStatus.InProgress);

        var withMetrics = await GetSprintAsync(client, team, sprint.Id);

        Assert.Equal(4, withMetrics.Metrics.Total);
        Assert.Equal(1, withMetrics.Metrics.Completed);
        Assert.Equal(3, withMetrics.Metrics.Remaining);
        Assert.Equal(25, withMetrics.Metrics.CompletionPercentage);
    }

    [Fact]
    public async Task TheListingCarriesTheMetricsOfEachSprint()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        var withIssues = await CreateSprintAsync(client, team, "Con issues");
        var empty = await CreateSprintAsync(client, team, "Vacío");

        var issue = await CreateIssueAsync(client, team, "Ya está");
        await PostAsync(client, IssueInSprintUrl(team, withIssues.Id, issue.Identifier));
        await ChangeStatusAsync(client, team, issue.Identifier, IssueStatus.Done);

        var page = await ListSprintsAsync(client, team);

        Assert.Equal(2, page.TotalCount);

        var listed = page.Items.Single(sprint => sprint.Id == withIssues.Id);
        Assert.Equal(1, listed.Metrics.Total);
        Assert.Equal(1, listed.Metrics.Completed);
        Assert.Equal(100, listed.Metrics.CompletionPercentage);

        var listedEmpty = page.Items.Single(sprint => sprint.Id == empty.Id);
        Assert.Equal(SprintMetrics.Empty, listedEmpty.Metrics);
    }

    [Fact]
    public async Task TheIssueDetailShowsItsSprint()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        var sprint = await CreateSprintAsync(client, team, "Sprint 12");
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        Assert.Null(issue.Sprint);

        await PostAsync(client, IssueInSprintUrl(team, sprint.Id, issue.Identifier));

        using var response = await client.GetAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}", CancellationToken.None);

        var refreshed = await response.Content.ReadFromJsonAsync<IssueResponse>();

        Assert.Equal(sprint.Id, refreshed!.Sprint!.Id);
        Assert.Equal("Sprint 12", refreshed.Sprint.Name);
    }

    [Fact]
    public async Task DeletingAnIssueLeavesTheSprintStanding()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        var sprint = await CreateSprintAsync(client, team, "Sprint 12");
        var issue = await CreateIssueAsync(client, team, "Se elimina");

        await PostAsync(client, IssueInSprintUrl(team, sprint.Id, issue.Identifier));

        using var deleted = await client.DeleteAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var afterDelete = await GetSprintAsync(client, team, sprint.Id);
        Assert.Empty(afterDelete.Issues);
        Assert.Equal(SprintMetrics.Empty, afterDelete.Metrics);
    }

    [Fact]
    public async Task DeletingATeamRemovesItsSprints()
    {
        var team = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        await CreateSprintAsync(client, team, "Se va con el equipo");

        using var deleted = await client.DeleteAsync($"/api/teams/{team.Key.Value}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Empty(dbContext.Sprints);
    }

    [Fact]
    public async Task AnyMemberCanPlanTheTeamsWork()
    {
        var team = await ATeamAsync(withMember: true);
        using var client = await SignInAsync(MemberEmail);

        var sprint = await CreateSprintAsync(client, team, "Sprint del member");
        var started = await PostAsync(client, $"{SprintUrl(team, sprint.Id)}/start");

        Assert.Equal(nameof(SprintStatus.Active), started.Status);
    }

    [Fact]
    public async Task SomeoneOutsideTheTeamSeesNoSprints()
    {
        var team = await ATeamAsync();
        using var ownerClient = await SignInAsync(OwnerEmail);
        var sprint = await CreateSprintAsync(ownerClient, team, "Interno");

        await AuthenticationScenario.CreateUserAsync(_factory, OutsiderEmail);
        using var outsiderClient = await AuthenticationScenario.SignInAsync(_factory, OutsiderEmail);

        using var list = await outsiderClient.GetAsync(SprintsUrl(team), CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);

        using var detail = await outsiderClient.GetAsync(SprintUrl(team, sprint.Id), CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);

        using var start = await outsiderClient.PostAsJsonAsync(
            $"{SprintUrl(team, sprint.Id)}/start", new { }, CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, start.StatusCode);
    }

    [Fact]
    public async Task ASprintIsNotReachableThroughAnotherTeam()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        var web = await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id);
        var core = await TeamScenario.CreateTeamAsync(_factory, "CORE", owner.Id);

        using var client = await AuthenticationScenario.SignInAsync(_factory, OwnerEmail);
        var sprint = await CreateSprintAsync(client, web, "Sprint Web");

        using var response = await client.GetAsync(SprintUrl(core, sprint.Id), CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WithoutASessionTheSprintsApiRespondsUnauthorized()
    {
        using var client = AuthenticationScenario.CreateClient(_factory);

        using var response = await client.GetAsync("/api/teams/WEB/sprints", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string SprintsUrl(Team team) => $"/api/teams/{team.Key.Value}/sprints";

    private static string SprintUrl(Team team, Guid sprintId) => $"{SprintsUrl(team)}/{sprintId}";

    private static string IssueInSprintUrl(Team team, Guid sprintId, string identifier) =>
        $"{SprintUrl(team, sprintId)}/issues/{identifier}";

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

    private static async Task<SprintResponse> CreateSprintAsync(HttpClient client, Team team, string name)
    {
        using var response = await client.PostAsJsonAsync(
            SprintsUrl(team),
            new { name, startDate = Start, endDate = End },
            CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<SprintResponse>())!;
    }

    private static async Task<SprintResponse> GetSprintAsync(HttpClient client, Team team, Guid sprintId)
    {
        using var response = await client.GetAsync(SprintUrl(team, sprintId), CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<SprintResponse>())!;
    }

    private static async Task<PagedResult<SprintSummaryResponse>> ListSprintsAsync(HttpClient client, Team team)
    {
        using var response = await client.GetAsync(SprintsUrl(team), CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<PagedResult<SprintSummaryResponse>>())!;
    }

    private static async Task<SprintResponse> PostAsync(HttpClient client, string url)
    {
        using var response = await client.PostAsJsonAsync(url, new { }, CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<SprintResponse>())!;
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
}
