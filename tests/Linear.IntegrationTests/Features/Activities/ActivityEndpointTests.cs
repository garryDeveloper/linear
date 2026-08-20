using System.Net;
using System.Net.Http.Json;

using Linear.Domain.Activities;
using Linear.Domain.Issues;
using Linear.Domain.Roadmaps;
using Linear.Domain.Teams;
using Linear.IntegrationTests.Infrastructure;
using Linear.Web.Features.Activities.Contracts;
using Linear.Web.Features.Comments.Contracts;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Features.Roadmaps.Contracts;
using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Shared.Pagination;

using Microsoft.EntityFrameworkCore;

namespace Linear.IntegrationTests.Features.Activities;

/// <summary>
/// El historial, de punta a punta: que operar por el API deje registro, que el registro tenga
/// actor y fecha, y que los dos feeds lo devuelvan.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ActivityEndpointTests : IAsyncLifetime
{
    private const string OwnerEmail = "owner@linear.dev";
    private const string MemberEmail = "member@linear.dev";
    private const string OutsiderEmail = "outsider@linear.dev";

    private readonly PostgresFixture _postgres;
    private readonly DatabaseWebApplicationFactory _factory;

    public ActivityEndpointTests(PostgresFixture postgres)
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

    // ---- que las acciones dejen registro ------------------------------------------------

    [Fact]
    public async Task CreatingAnIssueIsRecorded()
    {
        var (team, owner) = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        var activity = Assert.Single(await TeamActivityAsync(client, team));

        Assert.Equal(nameof(ActivityAction.IssueCreated), activity.Action);
        Assert.Equal(nameof(ActivityEntityType.Issue), activity.EntityType);
        Assert.Equal(issue.Id, activity.EntityId);
        Assert.Equal(owner, activity.Actor.Id);
        Assert.Equal("WEB-1", activity.Payload["identifier"]);
        Assert.NotEqual(default, activity.CreatedAt);
    }

    [Theory]
    [InlineData(IssueStatus.Done, nameof(ActivityAction.IssueCompleted))]
    [InlineData(IssueStatus.Canceled, nameof(ActivityAction.IssueCanceled))]
    [InlineData(IssueStatus.InProgress, nameof(ActivityAction.IssueUpdated))]
    public async Task ChangingStatusIsRecordedWithTheRightAction(IssueStatus status, string expected)
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        await ChangeStatusAsync(client, team, issue.Identifier, status);

        var activity = (await IssueActivityAsync(client, team, issue.Identifier))[0];

        Assert.Equal(expected, activity.Action);
        Assert.Equal(nameof(IssueStatus.Backlog), activity.Payload["oldValue"]);
        Assert.Equal(status.ToString(), activity.Payload["newValue"]);
    }

    [Fact]
    public async Task AssigningIsRecorded()
    {
        var (team, owner) = await ATeamAsync(withMember: true);
        using var client = await SignInAsync(OwnerEmail);
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        using var response = await client.PutAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/assignee",
            new { assigneeId = owner },
            CancellationToken.None);

        response.EnsureSuccessStatusCode();

        var activity = (await IssueActivityAsync(client, team, issue.Identifier))[0];

        Assert.Equal(nameof(ActivityAction.IssueAssigned), activity.Action);
        Assert.Equal(owner.ToString(), activity.Payload["newValue"]);
    }

    [Fact]
    public async Task AddingAndRemovingALabelAreRecorded()
    {
        var (team, _) = await ATeamAsync();
        var label = await TeamScenario.CreateLabelAsync(_factory, team.Id, "bug");

        using var client = await SignInAsync(OwnerEmail);
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        using var added = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/labels",
            new { labelId = label.Id },
            CancellationToken.None);
        added.EnsureSuccessStatusCode();

        using var removed = await client.DeleteAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/labels/{label.Id}",
            CancellationToken.None);
        removed.EnsureSuccessStatusCode();

        var actions = (await IssueActivityAsync(client, team, issue.Identifier))
            .Select(activity => activity.Action)
            .ToArray();

        Assert.Equal(
            [
                nameof(ActivityAction.LabelRemoved),
                nameof(ActivityAction.LabelAdded),
                nameof(ActivityAction.IssueCreated)
            ],
            actions);
    }

    /// <summary>
    /// Un comentario no conoce su equipo: lo resuelve el interceptor siguiendo el issue. Si
    /// eso fallara, la entrada no aparecería en el feed del equipo.
    /// </summary>
    [Fact]
    public async Task CommentingIsRecordedAgainstTheTeamOfTheIssue()
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        await CreateCommentAsync(client, team, issue.Identifier, "Reproduje el bug.");

        var activity = (await TeamActivityAsync(client, team))[0];

        Assert.Equal(nameof(ActivityAction.CommentCreated), activity.Action);
        Assert.Equal(nameof(ActivityEntityType.Comment), activity.EntityType);
    }

    [Fact]
    public async Task EditingACommentIsRecorded()
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");
        var comment = await CreateCommentAsync(client, team, issue.Identifier, "Original");

        using var response = await client.PutAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/comments/{comment.Id}",
            new { content = "Corregido" },
            CancellationToken.None);
        response.EnsureSuccessStatusCode();

        Assert.Equal(
            nameof(ActivityAction.CommentUpdated),
            (await TeamActivityAsync(client, team))[0].Action);
    }

    [Fact]
    public async Task StartingAndCompletingASprintAreRecorded()
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        using var created = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/sprints",
            new { name = "Sprint 12", startDate = "2026-09-01", endDate = "2026-09-15" },
            CancellationToken.None);
        var sprint = (await created.Content.ReadFromJsonAsync<SprintResponse>())!;

        await PostAsync(client, $"/api/teams/{team.Key.Value}/sprints/{sprint.Id}/start");
        await PostAsync(client, $"/api/teams/{team.Key.Value}/sprints/{sprint.Id}/complete");

        var actions = (await TeamActivityAsync(client, team))
            .Select(activity => activity.Action)
            .ToArray();

        // Crear el sprint no deja registro: la task 011 no define esa acción.
        Assert.Equal(
            [nameof(ActivityAction.SprintCompleted), nameof(ActivityAction.SprintStarted)],
            actions);
    }

    [Fact]
    public async Task CreatingAndUpdatingARoadmapItemAreRecorded()
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        using var createdRoadmap = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/roadmaps", new { name = "Segundo semestre" }, CancellationToken.None);
        var roadmap = (await createdRoadmap.Content.ReadFromJsonAsync<RoadmapResponse>())!;

        using var createdItem = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/roadmaps/{roadmap.Id}/items",
            new { name = "Autenticación", startDate = "2026-09-01", targetDate = "2026-11-30" },
            CancellationToken.None);
        var item = Assert.Single((await createdItem.Content.ReadFromJsonAsync<RoadmapResponse>())!.Items);

        using var updated = await client.PutAsJsonAsync(
            $"/api/teams/{team.Key.Value}/roadmaps/{roadmap.Id}/items/{item.Id}",
            new
            {
                name = "SSO",
                startDate = "2026-09-01",
                targetDate = "2026-11-30",
                status = nameof(RoadmapItemStatus.InProgress)
            },
            CancellationToken.None);
        updated.EnsureSuccessStatusCode();

        var activities = await TeamActivityAsync(client, team);

        Assert.Equal(nameof(ActivityAction.RoadmapItemUpdated), activities[0].Action);
        Assert.Equal("SSO", activities[0].Payload["name"]);
        Assert.Equal(nameof(ActivityEntityType.RoadmapItem), activities[0].EntityType);
        Assert.Equal(item.Id, activities[0].EntityId);

        Assert.Equal(nameof(ActivityAction.RoadmapItemCreated), activities[1].Action);
    }

    // ---- el feed del issue ---------------------------------------------------------------

    /// <summary>
    /// El historial de un issue incluye lo que pasó en sus comentarios, que no se puede pedir
    /// por EntityId porque esas entradas apuntan al comentario.
    /// </summary>
    [Fact]
    public async Task TheIssueFeedIncludesItsComments()
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        await CreateCommentAsync(client, team, issue.Identifier, "Reproduje el bug.");
        await ChangeStatusAsync(client, team, issue.Identifier, IssueStatus.Done);

        var actions = (await IssueActivityAsync(client, team, issue.Identifier))
            .Select(activity => activity.Action)
            .ToArray();

        Assert.Equal(
            [
                nameof(ActivityAction.IssueCompleted),
                nameof(ActivityAction.CommentCreated),
                nameof(ActivityAction.IssueCreated)
            ],
            actions);
    }

    /// <summary>El feed de un issue no trae lo que pasó en otro.</summary>
    [Fact]
    public async Task TheIssueFeedOnlyShowsItsOwnActivity()
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        var first = await CreateIssueAsync(client, team, "Primero");
        var second = await CreateIssueAsync(client, team, "Segundo");

        await CreateCommentAsync(client, team, second.Identifier, "Sobre el segundo");
        await ChangeStatusAsync(client, team, second.Identifier, IssueStatus.Done);

        var activities = await IssueActivityAsync(client, team, first.Identifier);

        var only = Assert.Single(activities);
        Assert.Equal(nameof(ActivityAction.IssueCreated), only.Action);
        Assert.Equal(first.Id, only.EntityId);
    }

    [Fact]
    public async Task TheFeedsAreOrderedNewestFirst()
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        await ChangeStatusAsync(client, team, issue.Identifier, IssueStatus.InProgress);
        await ChangeStatusAsync(client, team, issue.Identifier, IssueStatus.Done);

        var activities = await TeamActivityAsync(client, team);

        Assert.True(activities[0].CreatedAt >= activities[1].CreatedAt);
        Assert.True(activities[1].CreatedAt >= activities[2].CreatedAt);
    }

    [Fact]
    public async Task TheFeedIsPaginated()
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        for (var index = 1; index <= 5; index++)
        {
            await CreateIssueAsync(client, team, $"Issue {index}");
        }

        using var response = await client.GetAsync(
            $"/api/teams/{team.Key.Value}/activity?pageSize=2", CancellationToken.None);

        var page = (await response.Content.ReadFromJsonAsync<PagedResult<ActivityResponse>>())!;

        Assert.Equal(5, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.True(page.HasNextPage);
    }

    // ---- append-only ----------------------------------------------------------------------

    /// <summary>
    /// La actividad se guarda en la misma transacción que el cambio: es lo que evita que el
    /// historial quede desfasado del dato que describe.
    /// </summary>
    [Fact]
    public async Task AFailedOperationLeavesNoActivity()
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues", new { title = "   " }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await TeamActivityAsync(client, team));
    }

    /// <summary>
    /// El historial sobrevive al issue: es lo que lo vuelve útil para auditar. Eliminar el
    /// issue no borra lo que quedó registrado sobre él.
    /// </summary>
    [Fact]
    public async Task DeletingAnIssueKeepsItsActivity()
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var issue = await CreateIssueAsync(client, team, "Se elimina");

        using var deleted = await client.DeleteAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        Assert.NotEmpty(await TeamActivityAsync(client, team));
    }

    [Fact]
    public async Task ThereIsNoWayToChangeOrDeleteActivity()
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        var issue = await CreateIssueAsync(client, team, "Arreglar el login");

        var activity = Assert.Single(await TeamActivityAsync(client, team));

        // No hay endpoints de escritura sobre el historial: solo se lee. El código exacto lo
        // decide el enrutado —404 si no hay ruta, 405 si la hay pero no para ese verbo—; lo
        // que importa es que ninguno de los dos prospere.
        using var put = await client.PutAsJsonAsync(
            $"/api/teams/{team.Key.Value}/activity/{activity.Id}", new { }, CancellationToken.None);
        using var delete = await client.DeleteAsync(
            $"/api/teams/{team.Key.Value}/activity/{activity.Id}", CancellationToken.None);

        Assert.False(put.IsSuccessStatusCode);
        Assert.False(delete.IsSuccessStatusCode);

        // Y el registro sigue ahí, intacto.
        Assert.Single(await TeamActivityAsync(client, team));
        Assert.Equal(issue.Id, activity.EntityId);
    }

    [Fact]
    public async Task DeletingATeamRemovesItsActivity()
    {
        var (team, _) = await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);
        await CreateIssueAsync(client, team, "Se va con el equipo");

        using var deleted = await client.DeleteAsync(
            $"/api/teams/{team.Key.Value}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<Web.Infrastructure.Persistence.AppDbContext>();

        Assert.Empty(dbContext.Activities);
    }

    // ---- actor y aislamiento ---------------------------------------------------------------

    [Fact]
    public async Task EachEntryCarriesWhoDidIt()
    {
        var (team, owner) = await ATeamAsync(withMember: true);

        using var ownerClient = await SignInAsync(OwnerEmail);
        var issue = await CreateIssueAsync(ownerClient, team, "Arreglar el login");

        using var memberClient = await SignInAsync(MemberEmail);
        await CreateCommentAsync(memberClient, team, issue.Identifier, "Comento yo");

        var activities = await TeamActivityAsync(ownerClient, team);

        Assert.Equal(nameof(ActivityAction.CommentCreated), activities[0].Action);
        Assert.NotEqual(owner, activities[0].Actor.Id);
        Assert.Equal(owner, activities[1].Actor.Id);
    }

    [Fact]
    public async Task SomeoneOutsideTheTeamSeesNoActivity()
    {
        var (team, _) = await ATeamAsync();
        using var ownerClient = await SignInAsync(OwnerEmail);
        var issue = await CreateIssueAsync(ownerClient, team, "Arreglar el login");

        await AuthenticationScenario.CreateUserAsync(_factory, OutsiderEmail);
        using var outsiderClient = await AuthenticationScenario.SignInAsync(_factory, OutsiderEmail);

        using var teamFeed = await outsiderClient.GetAsync(
            $"/api/teams/{team.Key.Value}/activity", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, teamFeed.StatusCode);

        using var issueFeed = await outsiderClient.GetAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/activity", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, issueFeed.StatusCode);
    }

    /// <summary>El historial de un equipo no se mezcla con el de otro.</summary>
    [Fact]
    public async Task EachTeamHasItsOwnFeed()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        var web = await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id, "Web");
        var core = await TeamScenario.CreateTeamAsync(_factory, "CORE", owner.Id, "Core");

        using var client = await AuthenticationScenario.SignInAsync(_factory, OwnerEmail);

        await CreateIssueAsync(client, web, "En Web");
        await CreateIssueAsync(client, core, "En Core");

        var webFeed = Assert.Single(await TeamActivityAsync(client, web));
        var coreFeed = Assert.Single(await TeamActivityAsync(client, core));

        Assert.Equal("WEB-1", webFeed.Payload["identifier"]);
        Assert.Equal("CORE-1", coreFeed.Payload["identifier"]);
    }

    [Fact]
    public async Task WithoutASessionTheActivityApiRespondsUnauthorized()
    {
        using var client = AuthenticationScenario.CreateClient(_factory);

        using var response = await client.GetAsync("/api/teams/WEB/activity", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- escenario --------------------------------------------------------------------------

    private async Task<(Team Team, Guid Owner)> ATeamAsync(bool withMember = false)
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        var team = await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id, "Web");

        if (withMember)
        {
            var member = await AuthenticationScenario.CreateUserAsync(_factory, MemberEmail);
            await TeamScenario.AddMemberAsync(_factory, team.Id, member.Id, TeamRole.Member);
        }

        return (team, owner.Id);
    }

    private Task<HttpClient> SignInAsync(string email) =>
        AuthenticationScenario.SignInAsync(_factory, email);

    private static async Task<IReadOnlyList<ActivityResponse>> TeamActivityAsync(
        HttpClient client,
        Team team)
    {
        using var response = await client.GetAsync(
            $"/api/teams/{team.Key.Value}/activity", CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<PagedResult<ActivityResponse>>())!.Items;
    }

    private static async Task<IReadOnlyList<ActivityResponse>> IssueActivityAsync(
        HttpClient client,
        Team team,
        string identifier)
    {
        using var response = await client.GetAsync(
            $"/api/teams/{team.Key.Value}/issues/{identifier}/activity", CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<PagedResult<ActivityResponse>>())!.Items;
    }

    private static async Task<IssueResponse> CreateIssueAsync(HttpClient client, Team team, string title)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues", new { title }, CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<IssueResponse>())!;
    }

    private static async Task<CommentResponse> CreateCommentAsync(
        HttpClient client,
        Team team,
        string identifier,
        string content)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{identifier}/comments",
            new { content },
            CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<CommentResponse>())!;
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

    private static async Task PostAsync(HttpClient client, string url)
    {
        using var response = await client.PostAsJsonAsync(url, new { }, CancellationToken.None);

        response.EnsureSuccessStatusCode();
    }
}
