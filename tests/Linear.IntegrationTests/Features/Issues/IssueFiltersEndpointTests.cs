using System.Net;
using System.Net.Http.Json;

using Linear.Domain.Issues;
using Linear.Domain.Teams;
using Linear.IntegrationTests.Infrastructure;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Features.Sprints.Contracts;
using Linear.Web.Shared.Pagination;

namespace Linear.IntegrationTests.Features.Issues;

/// <summary>
/// Los filtros del listado, contra PostgreSQL real: es donde se ve de verdad cómo se
/// comportan los operadores negados frente a las columnas que admiten nulos.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class IssueFiltersEndpointTests : IAsyncLifetime
{
    private const string OwnerEmail = "owner@linear.dev";
    private const string MemberEmail = "member@linear.dev";

    private readonly PostgresFixture _postgres;
    private readonly DatabaseWebApplicationFactory _factory;

    public IssueFiltersEndpointTests(PostgresFixture postgres)
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
    public async Task WithoutFiltersEveryIssueIsListed()
    {
        await AScenarioAsync();

        var identifiers = await FilterAsync(string.Empty);

        Assert.Equal(5, identifiers.Length);
    }

    // ---- is / is not -------------------------------------------------------------------

    [Fact]
    public async Task StatusIs()
    {
        await AScenarioAsync();

        Assert.Equal(["WEB-2"], await FilterAsync("status=InProgress"));
    }

    [Fact]
    public async Task StatusIsNot()
    {
        await AScenarioAsync();

        var identifiers = await FilterAsync("status=not:Done");

        Assert.DoesNotContain("WEB-4", identifiers);
        Assert.Equal(4, identifiers.Length);
    }

    [Fact]
    public async Task TheStatusValueDoesNotDependOnCasing()
    {
        await AScenarioAsync();

        Assert.Equal(["WEB-2"], await FilterAsync("status=inprogress"));
    }

    // ---- in / not in -------------------------------------------------------------------

    [Fact]
    public async Task PriorityIn()
    {
        await AScenarioAsync();

        var identifiers = await FilterAsync("priority=in:High,Urgent");

        Assert.Equal(["WEB-2", "WEB-1"], identifiers);
    }

    [Fact]
    public async Task PriorityNotIn()
    {
        await AScenarioAsync();

        var identifiers = await FilterAsync("priority=not:High,Urgent");

        Assert.DoesNotContain("WEB-1", identifiers);
        Assert.DoesNotContain("WEB-2", identifiers);
        Assert.Equal(3, identifiers.Length);
    }

    /// <summary>Sin prefijo y con varios valores, la lectura natural es "in".</summary>
    [Fact]
    public async Task SeveralValuesWithoutAPrefixBehaveAsIn()
    {
        await AScenarioAsync();

        Assert.Equal(
            await FilterAsync("priority=in:High,Urgent"),
            await FilterAsync("priority=High,Urgent"));
    }

    // ---- contains ----------------------------------------------------------------------

    [Fact]
    public async Task TitleContains()
    {
        await AScenarioAsync();

        Assert.Equal(["WEB-1"], await FilterAsync("title=login"));
    }

    [Fact]
    public async Task TitleContainsDoesNotDependOnCasing()
    {
        await AScenarioAsync();

        Assert.Equal(["WEB-1"], await FilterAsync("title=LOGIN"));
    }

    /// <summary>
    /// Un comodín escrito por quien filtra es un carácter común, no "cualquier cosa": si no
    /// se escapara, este filtro devolvería todos los issues.
    /// </summary>
    [Fact]
    public async Task TheWildcardsOfLikeAreEscaped()
    {
        await AScenarioAsync();

        Assert.Empty(await FilterAsync("title=%"));
        Assert.Empty(await FilterAsync("title=_"));
    }

    // ---- assignee ----------------------------------------------------------------------

    [Fact]
    public async Task AssigneeIsMe()
    {
        await AScenarioAsync();

        // La sesión es la del owner, y WEB-1 y WEB-3 están asignados a él.
        Assert.Equal(["WEB-3", "WEB-1"], await FilterAsync("assignee=me"));
    }

    [Fact]
    public async Task AssigneeIsAParticularUser()
    {
        var scenario = await AScenarioAsync();

        Assert.Equal(["WEB-2"], await FilterAsync($"assignee={scenario.MemberId}"));
    }

    [Fact]
    public async Task AssigneeIsNone()
    {
        await AScenarioAsync();

        Assert.Equal(["WEB-5", "WEB-4"], await FilterAsync("assignee=none"));
    }

    /// <summary>
    /// Un issue sin responsable tampoco está asignado a esa persona, así que tiene que
    /// aparecer. En SQL, comparar una columna nula contra un valor da NULL y la descartaría:
    /// que esto pase es lo que confirma que la condición se niega entera.
    /// </summary>
    [Fact]
    public async Task AssigneeIsNotIncludesUnassignedIssues()
    {
        var scenario = await AScenarioAsync();

        var identifiers = await FilterAsync($"assignee=not:{scenario.MemberId}");

        Assert.Contains("WEB-4", identifiers);
        Assert.Contains("WEB-5", identifiers);
        Assert.DoesNotContain("WEB-2", identifiers);
    }

    [Fact]
    public async Task AssigneeIsNotNoneLeavesOnlyAssignedIssues()
    {
        await AScenarioAsync();

        Assert.Equal(["WEB-3", "WEB-2", "WEB-1"], await FilterAsync("assignee=not:none"));
    }

    [Fact]
    public async Task AssigneeInMixesMeWithOtherUsers()
    {
        var scenario = await AScenarioAsync();

        var identifiers = await FilterAsync($"assignee=in:me,{scenario.MemberId}");

        Assert.Equal(["WEB-3", "WEB-2", "WEB-1"], identifiers);
    }

    // ---- createdBy ---------------------------------------------------------------------

    [Fact]
    public async Task CreatedByMe()
    {
        await AScenarioAsync();

        Assert.Equal(5, (await FilterAsync("createdBy=me")).Length);
    }

    [Fact]
    public async Task CreatedByAnotherUserFindsNothing()
    {
        var scenario = await AScenarioAsync();

        Assert.Empty(await FilterAsync($"createdBy={scenario.MemberId}"));
    }

    /// <summary>Todo issue tiene autor, así que "creado por nadie" no es un valor válido.</summary>
    [Fact]
    public async Task CreatedByNoneIsRejected()
    {
        await AScenarioAsync();

        Assert.Equal(HttpStatusCode.BadRequest, await FilterStatusAsync("createdBy=none"));
    }

    // ---- label -------------------------------------------------------------------------

    [Fact]
    public async Task LabelIsByName()
    {
        await AScenarioAsync();

        Assert.Equal(["WEB-1"], await FilterAsync("label=bug"));
    }

    [Fact]
    public async Task LabelIsById()
    {
        var scenario = await AScenarioAsync();

        Assert.Equal(["WEB-1"], await FilterAsync($"label={scenario.BugLabelId}"));
    }

    [Fact]
    public async Task LabelNamesDoNotDependOnCasing()
    {
        await AScenarioAsync();

        Assert.Equal(["WEB-1"], await FilterAsync("label=BUG"));
    }

    /// <summary>Un issue sin labels tampoco tiene la label "bug": tiene que aparecer.</summary>
    [Fact]
    public async Task LabelIsNotIncludesIssuesWithoutLabels()
    {
        await AScenarioAsync();

        var identifiers = await FilterAsync("label=not:bug");

        Assert.DoesNotContain("WEB-1", identifiers);
        Assert.Equal(4, identifiers.Length);
    }

    [Fact]
    public async Task LabelIn()
    {
        await AScenarioAsync();

        var identifiers = await FilterAsync("label=in:bug,mejora");

        Assert.Equal(["WEB-2", "WEB-1"], identifiers);
    }

    /// <summary>
    /// Un issue con dos labels que coinciden aparece una sola vez: la condición es un EXISTS,
    /// no un JOIN que multiplicaría la fila.
    /// </summary>
    [Fact]
    public async Task AnIssueWithSeveralMatchingLabelsIsListedOnce()
    {
        var scenario = await AScenarioAsync();

        using var client = await SignInAsync();
        await AddLabelAsync(client, "WEB-1", scenario.ImprovementLabelId);

        var identifiers = await FilterAsync("label=in:bug,mejora");

        Assert.Equal(["WEB-2", "WEB-1"], identifiers);
    }

    [Fact]
    public async Task AnUnknownLabelNameIsRejected()
    {
        await AScenarioAsync();

        Assert.Equal(HttpStatusCode.BadRequest, await FilterStatusAsync("label=inexistente"));
    }

    // ---- sprint ------------------------------------------------------------------------

    [Fact]
    public async Task SprintIs()
    {
        var scenario = await AScenarioAsync();

        Assert.Equal(["WEB-2", "WEB-1"], await FilterAsync($"sprint={scenario.SprintId}"));
    }

    [Fact]
    public async Task SprintIsNone()
    {
        await AScenarioAsync();

        Assert.Equal(["WEB-5", "WEB-4", "WEB-3"], await FilterAsync("sprint=none"));
    }

    [Fact]
    public async Task SprintIsNotIncludesIssuesWithoutSprint()
    {
        var scenario = await AScenarioAsync();

        var identifiers = await FilterAsync($"sprint=not:{scenario.SprintId}");

        Assert.Equal(["WEB-5", "WEB-4", "WEB-3"], identifiers);
    }

    /// <summary>
    /// El sprint de otro equipo no filtra por él ni confirma que exista: simplemente no hay
    /// issues de este equipo en un sprint ajeno.
    /// </summary>
    [Fact]
    public async Task ASprintFromAnotherTeamMatchesNothing()
    {
        var scenario = await AScenarioAsync();

        using var client = await SignInAsync();
        var other = await TeamScenario.CreateTeamAsync(_factory, "CORE", scenario.OwnerId);
        var foreignSprint = await CreateSprintAsync(client, other.Key.Value);

        Assert.Empty(await FilterAsync($"sprint={foreignSprint.Id}"));
    }

    // ---- combinaciones -----------------------------------------------------------------

    [Fact]
    public async Task SeveralFiltersCombineWithAnd()
    {
        await AScenarioAsync();

        // WEB-1: Todo, Urgent, asignado al owner. WEB-3 también es del owner pero es InReview.
        Assert.Equal(["WEB-1"], await FilterAsync("status=Todo&assignee=me&priority=Urgent"));
    }

    [Fact]
    public async Task CombiningFiltersThatShareNoIssueReturnsNothing()
    {
        await AScenarioAsync();

        Assert.Empty(await FilterAsync("status=Done&assignee=me"));
    }

    /// <summary>
    /// El total paginado es el de la consulta filtrada, no el del equipo entero: si no, la
    /// paginación ofrecería páginas vacías.
    /// </summary>
    [Fact]
    public async Task TheTotalCountReflectsTheFilter()
    {
        await AScenarioAsync();

        var page = await FilterPageAsync("assignee=none");

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
    }

    [Fact]
    public async Task FiltersApplyOnTopOfThePagination()
    {
        await AScenarioAsync();

        var first = await FilterPageAsync("createdBy=me&pageSize=2");

        Assert.Equal(5, first.TotalCount);
        Assert.Equal(2, first.Items.Count);
        Assert.True(first.HasNextPage);
    }

    // ---- errores -----------------------------------------------------------------------

    [Theory]
    [InlineData("status=Inventado")]
    [InlineData("status=99")]
    [InlineData("priority=Altisima")]
    [InlineData("status=between:A,B")]
    [InlineData("status=is:Todo,Done")]
    [InlineData("status=contains:Prog")]
    [InlineData("title=is:login")]
    [InlineData("assignee=no-es-un-guid")]
    public async Task AnInvalidFilterIsRejected(string query)
    {
        await AScenarioAsync();

        Assert.Equal(HttpStatusCode.BadRequest, await FilterStatusAsync(query));
    }

    /// <summary>Los archivados siguen fuera por omisión, aunque coincidan con el filtro.</summary>
    [Fact]
    public async Task FiltersDoNotResurrectArchivedIssues()
    {
        await AScenarioAsync();

        using var client = await SignInAsync();
        await client.PostAsJsonAsync(
            $"/api/teams/WEB/issues/WEB-1/archive", new { }, CancellationToken.None);

        Assert.Empty(await FilterAsync("title=login"));
        Assert.Equal(["WEB-1"], await FilterAsync("title=login&includeArchived=true"));
    }

    // ---- escenario ---------------------------------------------------------------------

    private async Task<string[]> FilterAsync(string query) =>
        [.. (await FilterPageAsync(query)).Items.Select(issue => issue.Identifier)];

    private async Task<PagedResult<IssueSummaryResponse>> FilterPageAsync(string query)
    {
        using var client = await SignInAsync();

        using var response = await client.GetAsync($"/api/teams/WEB/issues?{query}", CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<PagedResult<IssueSummaryResponse>>())!;
    }

    private async Task<HttpStatusCode> FilterStatusAsync(string query)
    {
        using var client = await SignInAsync();

        using var response = await client.GetAsync($"/api/teams/WEB/issues?{query}", CancellationToken.None);

        return response.StatusCode;
    }

    private Task<HttpClient> SignInAsync() => AuthenticationScenario.SignInAsync(_factory, OwnerEmail);

    /// <summary>
    /// Cinco issues del equipo WEB que cubren cada combinación que los filtros tienen que
    /// distinguir: con y sin responsable, con y sin label, con y sin sprint.
    /// </summary>
    private async Task<Scenario> AScenarioAsync()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        var member = await AuthenticationScenario.CreateUserAsync(_factory, MemberEmail);

        var team = await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id, "Web");
        await TeamScenario.AddMemberAsync(_factory, team.Id, member.Id, TeamRole.Member);

        var bug = await TeamScenario.CreateLabelAsync(_factory, team.Id, "bug");
        var improvement = await TeamScenario.CreateLabelAsync(_factory, team.Id, "mejora");

        using var client = await SignInAsync();

        var sprint = await CreateSprintAsync(client, "WEB");

        // WEB-1: Todo, Urgent, owner, label bug, en el sprint, título con "login".
        await CreateIssueAsync(client, "Arreglar el login", owner.Id, [bug.Id]);
        await ChangeStatusAsync(client, "WEB-1", IssueStatus.Todo);
        await ChangePriorityAsync(client, "WEB-1", IssuePriority.Urgent);
        await AddToSprintAsync(client, sprint.Id, "WEB-1");

        // WEB-2: InProgress, High, member, label mejora, en el sprint.
        await CreateIssueAsync(client, "Migrar el carrito", member.Id, [improvement.Id]);
        await ChangeStatusAsync(client, "WEB-2", IssueStatus.InProgress);
        await ChangePriorityAsync(client, "WEB-2", IssuePriority.High);
        await AddToSprintAsync(client, sprint.Id, "WEB-2");

        // WEB-3: InReview, Medium, owner, sin labels, sin sprint.
        await CreateIssueAsync(client, "Revisar el pago", owner.Id, []);
        await ChangeStatusAsync(client, "WEB-3", IssueStatus.InReview);
        await ChangePriorityAsync(client, "WEB-3", IssuePriority.Medium);

        // WEB-4: Done, sin prioridad, sin responsable, sin labels, sin sprint.
        await CreateIssueAsync(client, "Pulir el resumen", assigneeId: null, []);
        await ChangeStatusAsync(client, "WEB-4", IssueStatus.Done);

        // WEB-5: Backlog, sin nada.
        await CreateIssueAsync(client, "Idea suelta", assigneeId: null, []);

        return new Scenario(owner.Id, member.Id, bug.Id, improvement.Id, sprint.Id);
    }

    private static async Task CreateIssueAsync(
        HttpClient client,
        string title,
        Guid? assigneeId,
        IReadOnlyList<Guid> labelIds)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/teams/WEB/issues", new { title, assigneeId, labelIds }, CancellationToken.None);

        response.EnsureSuccessStatusCode();
    }

    private static async Task ChangeStatusAsync(HttpClient client, string identifier, IssueStatus status)
    {
        using var response = await client.PutAsJsonAsync(
            $"/api/teams/WEB/issues/{identifier}/status",
            new { status = status.ToString() },
            CancellationToken.None);

        response.EnsureSuccessStatusCode();
    }

    private static async Task ChangePriorityAsync(HttpClient client, string identifier, IssuePriority priority)
    {
        using var response = await client.PutAsJsonAsync(
            $"/api/teams/WEB/issues/{identifier}/priority",
            new { priority = priority.ToString() },
            CancellationToken.None);

        response.EnsureSuccessStatusCode();
    }

    private static async Task AddLabelAsync(HttpClient client, string identifier, Guid labelId)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/teams/WEB/issues/{identifier}/labels",
            new { labelId },
            CancellationToken.None);

        response.EnsureSuccessStatusCode();
    }

    private static async Task<SprintResponse> CreateSprintAsync(HttpClient client, string teamKey)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{teamKey}/sprints",
            new { name = "Sprint 12", startDate = "2026-08-19", endDate = "2026-09-02" },
            CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<SprintResponse>())!;
    }

    private static async Task AddToSprintAsync(HttpClient client, Guid sprintId, string identifier)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/teams/WEB/sprints/{sprintId}/issues/{identifier}", new { }, CancellationToken.None);

        response.EnsureSuccessStatusCode();
    }

    private sealed record Scenario(
        Guid OwnerId,
        Guid MemberId,
        Guid BugLabelId,
        Guid ImprovementLabelId,
        Guid SprintId);
}
