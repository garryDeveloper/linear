using System.Net;
using System.Net.Http.Json;

using Linear.Domain.Issues;
using Linear.Domain.Teams;
using Linear.IntegrationTests.Infrastructure;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Shared.Pagination;
using Linear.Web.Shared.Results;

using Microsoft.EntityFrameworkCore;

namespace Linear.IntegrationTests.Features.Issues;

[Collection(PostgresCollection.Name)]
public sealed class IssuesEndpointTests : IAsyncLifetime
{
    private const string OwnerEmail = "owner@linear.dev";
    private const string AdminEmail = "admin@linear.dev";
    private const string MemberEmail = "member@linear.dev";
    private const string OutsiderEmail = "outsider@linear.dev";

    private readonly PostgresFixture _postgres;
    private readonly DatabaseWebApplicationFactory _factory;

    public IssuesEndpointTests(PostgresFixture postgres)
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
    public async Task CreatingAnIssueAssignsTheFirstIdentifier()
    {
        var (team, _, _, owner) = await ATeamWithTheThreeRolesAsync();
        using var client = await SignInAsync(OwnerEmail, createUser: false);

        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues",
            new { title = "Set up staging environment" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var issue = await response.Content.ReadFromJsonAsync<IssueResponse>();

        Assert.NotNull(issue);
        Assert.Equal("WEB-1", issue.Identifier);
        Assert.Equal(nameof(IssueStatus.Backlog), issue.Status);
        Assert.Equal(nameof(IssuePriority.None), issue.Priority);
        Assert.Equal(owner, issue.CreatedBy.Id);
    }

    [Fact]
    public async Task IdentifiersAreSequentialWithinATeam()
    {
        var (team, _, _, _) = await ATeamWithTheThreeRolesAsync();
        using var client = await SignInAsync(OwnerEmail, createUser: false);

        var first = await CreateIssueAsync(client, team.Key.Value, "Primero");
        var second = await CreateIssueAsync(client, team.Key.Value, "Segundo");
        var third = await CreateIssueAsync(client, team.Key.Value, "Tercero");

        Assert.Equal("WEB-1", first.Identifier);
        Assert.Equal("WEB-2", second.Identifier);
        Assert.Equal("WEB-3", third.Identifier);
    }

    [Fact]
    public async Task EachTeamHasItsOwnSequenceStartingAtOne()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        var web = await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id);
        var core = await TeamScenario.CreateTeamAsync(_factory, "CORE", owner.Id);

        using var client = await AuthenticationScenario.SignInAsync(_factory, OwnerEmail);

        var webIssue = await CreateIssueAsync(client, web.Key.Value, "En Web");
        var coreIssue = await CreateIssueAsync(client, core.Key.Value, "En Core");

        Assert.Equal("WEB-1", webIssue.Identifier);
        Assert.Equal("CORE-1", coreIssue.Identifier);
    }

    [Fact]
    public async Task ConcurrentCreationsNeverProduceTheSameIdentifier()
    {
        // Es la garantía central de la task: el número se reserva con un UPDATE ...
        // RETURNING atómico, así que ni siquiera treinta creaciones a la vez deberían
        // repetir o saltear un número.
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        var team = await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id);

        const int concurrentRequests = 30;

        var responses = await Task.WhenAll(Enumerable.Range(1, concurrentRequests).Select(async index =>
        {
            using var client = await AuthenticationScenario.SignInAsync(_factory, OwnerEmail);
            return await CreateIssueAsync(client, team.Key.Value, $"Issue concurrente {index}");
        }));

        var identifiers = responses.Select(issue => issue.Identifier).ToArray();
        var expected = Enumerable.Range(1, concurrentRequests).Select(number => $"WEB-{number}").ToHashSet();

        Assert.Equal(concurrentRequests, identifiers.Distinct().Count());
        Assert.Equal(expected, identifiers.ToHashSet());
    }

    [Fact]
    public async Task ANewIssueCanBeCreatedWithAnAssigneeAndLabels()
    {
        var (team, _, member, _) = await ATeamWithTheThreeRolesAsync();
        var label = await TeamScenario.CreateLabelAsync(_factory, team.Id, "bug");

        using var client = await SignInAsync(OwnerEmail, createUser: false);

        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues",
            new { title = "Con datos", assigneeId = member, labelIds = new[] { label.Id } },
            CancellationToken.None);

        var issue = await response.Content.ReadFromJsonAsync<IssueResponse>();

        Assert.Equal(member, issue!.Assignee?.Id);
        Assert.Single(issue.Labels);
        Assert.Equal("bug", issue.Labels[0].Name);
    }

    [Fact]
    public async Task AssigningSomeoneOutsideTheTeamFails()
    {
        var (team, _, _, _) = await ATeamWithTheThreeRolesAsync();
        var outsider = await AuthenticationScenario.CreateUserAsync(_factory, OutsiderEmail);

        using var client = await SignInAsync(OwnerEmail, createUser: false);

        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues",
            new { title = "Con datos", assigneeId = outsider.Id },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(IssueErrors.AssigneeNotAMember.Code, error!.Code);
    }

    [Fact]
    public async Task ALabelFromAnotherTeamIsRejectedAtCreation()
    {
        var (team, _, _, _) = await ATeamWithTheThreeRolesAsync();
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, "otro-owner@linear.dev");
        var otherTeam = await TeamScenario.CreateTeamAsync(_factory, "CORE", owner.Id);
        var foreignLabel = await TeamScenario.CreateLabelAsync(_factory, otherTeam.Id, "deuda");

        using var client = await SignInAsync(OwnerEmail, createUser: false);

        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues",
            new { title = "Con label ajena", labelIds = new[] { foreignLabel.Id } },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ATitleLessRequestIsRejected()
    {
        var (team, _, _, _) = await ATeamWithTheThreeRolesAsync();
        using var client = await SignInAsync(OwnerEmail, createUser: false);

        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues",
            new { title = "" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TheListingOnlyShowsIssuesOfTheTeamAndHidesArchivedByDefault()
    {
        var (team, _, _, _) = await ATeamWithTheThreeRolesAsync();
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, "otro-owner2@linear.dev");
        var otherTeam = await TeamScenario.CreateTeamAsync(_factory, "CORE", owner.Id);

        using var client = await SignInAsync(OwnerEmail, createUser: false);
        using var otherClient = await AuthenticationScenario.SignInAsync(_factory, "otro-owner2@linear.dev");

        var active = await CreateIssueAsync(client, team.Key.Value, "Activo");
        var toArchive = await CreateIssueAsync(client, team.Key.Value, "Para archivar");
        await CreateIssueAsync(otherClient, otherTeam.Key.Value, "De otro equipo");

        await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{toArchive.Identifier}/archive", new { }, CancellationToken.None);

        var page = await client.GetFromJsonAsync<PagedResult<IssueSummaryResponse>>(
            $"/api/teams/{team.Key.Value}/issues", CancellationToken.None);

        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal(active.Identifier, page.Items[0].Identifier);
    }

    [Fact]
    public async Task IncludingArchivedBringsBackTheArchivedOnes()
    {
        var (team, _, _, _) = await ATeamWithTheThreeRolesAsync();
        using var client = await SignInAsync(OwnerEmail, createUser: false);

        var issue = await CreateIssueAsync(client, team.Key.Value, "Para archivar");
        await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/archive", new { }, CancellationToken.None);

        var page = await client.GetFromJsonAsync<PagedResult<IssueSummaryResponse>>(
            $"/api/teams/{team.Key.Value}/issues?includeArchived=true", CancellationToken.None);

        Assert.Single(page!.Items);
    }

    [Fact]
    public async Task AnOutsiderCannotSeeTheIssue()
    {
        var (team, _, _, _) = await ATeamWithTheThreeRolesAsync();
        using var ownerClient = await SignInAsync(OwnerEmail, createUser: false);
        var issue = await CreateIssueAsync(ownerClient, team.Key.Value, "Privado");

        using var outsiderClient = await SignInAsync(OutsiderEmail);

        using var response = await outsiderClient.GetAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AnIssueCannotBeReachedThroughAnotherTeamsRoute()
    {
        var (team, _, _, _) = await ATeamWithTheThreeRolesAsync();
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, "owner3@linear.dev");
        var otherTeam = await TeamScenario.CreateTeamAsync(_factory, "CORE", owner.Id);

        using var client = await SignInAsync(OwnerEmail, createUser: false);
        var issue = await CreateIssueAsync(client, team.Key.Value, "De WEB");

        using var otherClient = await AuthenticationScenario.SignInAsync(_factory, "owner3@linear.dev");

        using var response = await otherClient.GetAsync(
            $"/api/teams/{otherTeam.Key.Value}/issues/{issue.Identifier}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TheTitleAndDescriptionCanBeUpdated()
    {
        var (team, _, member, _) = await ATeamWithTheThreeRolesAsync();
        using var ownerClient = await SignInAsync(OwnerEmail, createUser: false);
        var issue = await CreateIssueAsync(ownerClient, team.Key.Value, "Original");

        using var memberClient = await SignInAsync(MemberEmail, createUser: false);

        using var response = await memberClient.PutAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}",
            new { title = "Renombrado", description = "Nueva descripción" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.Equal("Renombrado", updated!.Title);
        Assert.Equal("Nueva descripción", updated.Description);
    }

    [Fact]
    public async Task MovingAnIssueToDoneSetsCompletedAt()
    {
        var (team, _, _, _) = await ATeamWithTheThreeRolesAsync();
        using var client = await SignInAsync(OwnerEmail, createUser: false);
        var issue = await CreateIssueAsync(client, team.Key.Value, "Para completar");

        using var response = await client.PutAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/status",
            new { status = nameof(IssueStatus.Done) },
            CancellationToken.None);

        var updated = await response.Content.ReadFromJsonAsync<IssueResponse>();

        Assert.Equal(nameof(IssueStatus.Done), updated!.Status);
        Assert.NotNull(updated.CompletedAt);
    }

    [Fact]
    public async Task ThePriorityCanBeChanged()
    {
        var (team, _, _, _) = await ATeamWithTheThreeRolesAsync();
        using var client = await SignInAsync(OwnerEmail, createUser: false);
        var issue = await CreateIssueAsync(client, team.Key.Value, "Urgente");

        using var response = await client.PutAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/priority",
            new { priority = nameof(IssuePriority.Urgent) },
            CancellationToken.None);

        var updated = await response.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.Equal(nameof(IssuePriority.Urgent), updated!.Priority);
    }

    [Fact]
    public async Task TheAssigneeCanBeClearedByPassingNull()
    {
        var (team, _, member, _) = await ATeamWithTheThreeRolesAsync();
        using var client = await SignInAsync(OwnerEmail, createUser: false);
        var issue = await CreateIssueAsync(client, team.Key.Value, "Con responsable", assigneeId: member);

        using var response = await client.PutAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/assignee",
            new { assigneeId = (Guid?)null },
            CancellationToken.None);

        var updated = await response.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.Null(updated!.Assignee);
    }

    [Fact]
    public async Task TheEstimateAcceptsAValidValue()
    {
        var (team, _, _, _) = await ATeamWithTheThreeRolesAsync();
        using var client = await SignInAsync(OwnerEmail, createUser: false);
        var issue = await CreateIssueAsync(client, team.Key.Value, "Con estimate");

        using var response = await client.PutAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/estimate",
            new { estimate = 5 },
            CancellationToken.None);

        var updated = await response.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.Equal(5, updated!.Estimate);
    }

    [Fact]
    public async Task AnEstimateOutOfRangeIsRejected()
    {
        var (team, _, _, _) = await ATeamWithTheThreeRolesAsync();
        using var client = await SignInAsync(OwnerEmail, createUser: false);
        var issue = await CreateIssueAsync(client, team.Key.Value, "Con estimate");

        using var response = await client.PutAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/estimate",
            new { estimate = -1 },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ALabelCanBeAddedAndRemoved()
    {
        var (team, _, _, _) = await ATeamWithTheThreeRolesAsync();
        var label = await TeamScenario.CreateLabelAsync(_factory, team.Id, "mejora");

        using var client = await SignInAsync(OwnerEmail, createUser: false);
        var issue = await CreateIssueAsync(client, team.Key.Value, "Sin labels");

        using var added = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/labels",
            new { labelId = label.Id },
            CancellationToken.None);

        var withLabel = await added.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.Single(withLabel!.Labels);

        using var removed = await client.DeleteAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/labels/{label.Id}", CancellationToken.None);

        var withoutLabel = await removed.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.Empty(withoutLabel!.Labels);
    }

    [Fact]
    public async Task AddingTheSameLabelTwiceFails()
    {
        var (team, _, _, _) = await ATeamWithTheThreeRolesAsync();
        var label = await TeamScenario.CreateLabelAsync(_factory, team.Id, "mejora");

        using var client = await SignInAsync(OwnerEmail, createUser: false);
        var issue = await CreateIssueAsync(client, team.Key.Value, "Sin labels", labelIds: [label.Id]);

        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/labels",
            new { labelId = label.Id },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ArchivingRemovesTheIssueFromTheDefaultListingButKeepsIt()
    {
        var (team, _, _, _) = await ATeamWithTheThreeRolesAsync();
        using var client = await SignInAsync(OwnerEmail, createUser: false);
        var issue = await CreateIssueAsync(client, team.Key.Value, "Para archivar");

        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/archive", new { }, CancellationToken.None);

        var archived = await response.Content.ReadFromJsonAsync<IssueResponse>();
        Assert.NotNull(archived!.ArchivedAt);

        // Sigue siendo alcanzable por identificador: archivar no es eliminar.
        using var stillThere = await client.GetAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, stillThere.StatusCode);
    }

    [Fact]
    public async Task ArchivingTwiceFails()
    {
        var (team, _, _, _) = await ATeamWithTheThreeRolesAsync();
        using var client = await SignInAsync(OwnerEmail, createUser: false);
        var issue = await CreateIssueAsync(client, team.Key.Value, "Para archivar");

        await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/archive", new { }, CancellationToken.None);

        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/archive", new { }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AMemberCanArchiveButNotDelete()
    {
        var (team, _, _, _) = await ATeamWithTheThreeRolesAsync();
        using var ownerClient = await SignInAsync(OwnerEmail, createUser: false);
        var issue = await CreateIssueAsync(ownerClient, team.Key.Value, "Del equipo");

        using var memberClient = await SignInAsync(MemberEmail, createUser: false);

        using var deleteAttempt = await memberClient.DeleteAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.Forbidden, deleteAttempt.StatusCode);

        using var archiveAttempt = await memberClient.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}/archive", new { }, CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, archiveAttempt.StatusCode);
    }

    [Fact]
    public async Task AnAdminCanDeleteAnIssuePermanently()
    {
        var (team, admin, _, _) = await ATeamWithTheThreeRolesAsync();
        using var ownerClient = await SignInAsync(OwnerEmail, createUser: false);
        var issue = await CreateIssueAsync(ownerClient, team.Key.Value, "A borrar");

        using var adminClient = await SignInAsync(AdminEmail, createUser: false);

        using var response = await adminClient.DeleteAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var afterDelete = await adminClient.GetAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue.Identifier}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task DeletingATeamRemovesItsIssues()
    {
        var (team, _, _, _) = await ATeamWithTheThreeRolesAsync();
        using var client = await SignInAsync(OwnerEmail, createUser: false);
        await CreateIssueAsync(client, team.Key.Value, "Se va con el equipo");

        using var deleted = await client.DeleteAsync($"/api/teams/{team.Key.Value}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Web.Infrastructure.Persistence.AppDbContext>();

        Assert.Empty(dbContext.Issues);
    }

    [Fact]
    public async Task WithoutASessionTheIssuesApiRespondsUnauthorized()
    {
        using var client = AuthenticationScenario.CreateClient(_factory);

        using var response = await client.GetAsync("/api/teams/WEB/issues", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<(Team Team, Guid Admin, Guid Member, Guid Owner)> ATeamWithTheThreeRolesAsync()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        var admin = await AuthenticationScenario.CreateUserAsync(_factory, AdminEmail);
        var member = await AuthenticationScenario.CreateUserAsync(_factory, MemberEmail);

        var team = await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id, "Web");

        await TeamScenario.AddMemberAsync(_factory, team.Id, admin.Id, TeamRole.Admin);
        await TeamScenario.AddMemberAsync(_factory, team.Id, member.Id, TeamRole.Member);

        return (team, admin.Id, member.Id, owner.Id);
    }

    private async Task<HttpClient> SignInAsync(string email, bool createUser = true)
    {
        if (createUser)
        {
            await AuthenticationScenario.CreateUserAsync(_factory, email);
        }

        return await AuthenticationScenario.SignInAsync(_factory, email);
    }

    private static async Task<IssueResponse> CreateIssueAsync(
        HttpClient client,
        string teamKey,
        string title,
        Guid? assigneeId = null,
        IReadOnlyList<Guid>? labelIds = null)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{teamKey}/issues",
            new { title, assigneeId, labelIds = labelIds ?? [] },
            CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<IssueResponse>())!;
    }
}
