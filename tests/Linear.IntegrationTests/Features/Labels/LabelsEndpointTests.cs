using System.Net;
using System.Net.Http.Json;

using Linear.Domain.Labels;
using Linear.Domain.Teams;
using Linear.IntegrationTests.Infrastructure;
using Linear.Web.Features.Labels.Contracts;
using Linear.Web.Shared.Pagination;
using Linear.Web.Shared.Results;

namespace Linear.IntegrationTests.Features.Labels;

[Collection(PostgresCollection.Name)]
public sealed class LabelsEndpointTests : IAsyncLifetime
{
    private const string OwnerEmail = "owner@linear.dev";
    private const string AdminEmail = "admin@linear.dev";
    private const string MemberEmail = "member@linear.dev";
    private const string OutsiderEmail = "outsider@linear.dev";

    private readonly PostgresFixture _postgres;
    private readonly DatabaseWebApplicationFactory _factory;

    public LabelsEndpointTests(PostgresFixture postgres)
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
    public async Task AnAdminCanCreateALabel()
    {
        await ATeamAsync();
        using var client = await SignInAsync(AdminEmail);

        using var response = await client.PostAsJsonAsync(
            "/api/teams/WEB/labels",
            new { name = "bug", description = "Algo no funciona", color = "#E5484D" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var label = await response.Content.ReadFromJsonAsync<LabelResponse>();

        Assert.NotNull(label);
        Assert.Equal("bug", label.Name);
        Assert.Equal("#E5484D", label.Color);
    }

    [Fact]
    public async Task WithoutAColorTheDefaultOneIsUsed()
    {
        await ATeamAsync();
        using var client = await SignInAsync(AdminEmail);

        using var response = await client.PostAsJsonAsync(
            "/api/teams/WEB/labels",
            new { name = "bug" },
            CancellationToken.None);

        var label = await response.Content.ReadFromJsonAsync<LabelResponse>();

        Assert.Equal(LabelColor.Default.Value, label!.Color);
    }

    [Fact]
    public async Task TheServerResolvesTheTextContrast()
    {
        await ATeamAsync();
        using var client = await SignInAsync(AdminEmail);

        using var light = await client.PostAsJsonAsync(
            "/api/teams/WEB/labels",
            new { name = "claro", color = "#FFFF00" },
            CancellationToken.None);

        using var dark = await client.PostAsJsonAsync(
            "/api/teams/WEB/labels",
            new { name = "oscuro", color = "#000000" },
            CancellationToken.None);

        Assert.True((await light.Content.ReadFromJsonAsync<LabelResponse>())!.PrefersDarkText);
        Assert.False((await dark.Content.ReadFromJsonAsync<LabelResponse>())!.PrefersDarkText);
    }

    [Fact]
    public async Task TheNameIsUniqueWithinTheTeamIgnoringCase()
    {
        await ATeamAsync();
        using var client = await SignInAsync(AdminEmail);

        await CreateLabelAsync(client, "WEB", "bug");

        using var duplicate = await client.PostAsJsonAsync(
            "/api/teams/WEB/labels",
            new { name = "BUG" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var error = await duplicate.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(LabelErrors.NameAlreadyExists.Code, error!.Code);
    }

    [Fact]
    public async Task TwoTeamsCanTenerLaMismaLabel()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id);
        await TeamScenario.CreateTeamAsync(_factory, "CORE", owner.Id);

        using var client = await AuthenticationScenario.SignInAsync(_factory, OwnerEmail);

        await CreateLabelAsync(client, "WEB", "bug");

        using var second = await client.PostAsJsonAsync(
            "/api/teams/CORE/labels",
            new { name = "bug" },
            CancellationToken.None);

        // Las labels están aisladas por equipo: el nombre solo tiene que ser único dentro
        // de cada uno.
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task TheListingOnlyShowsTheLabelsOfTheTeam()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id);
        await TeamScenario.CreateTeamAsync(_factory, "CORE", owner.Id);

        using var client = await AuthenticationScenario.SignInAsync(_factory, OwnerEmail);

        await CreateLabelAsync(client, "WEB", "bug");
        await CreateLabelAsync(client, "WEB", "mejora");
        await CreateLabelAsync(client, "CORE", "deuda");

        var labels = await client.GetFromJsonAsync<PagedResult<LabelResponse>>(
            "/api/teams/WEB/labels",
            CancellationToken.None);

        Assert.NotNull(labels);
        Assert.Equal(2, labels.TotalCount);
        Assert.DoesNotContain(labels.Items, label => label.Name == "deuda");
    }

    [Fact]
    public async Task AMemberCanReadTheLabels()
    {
        await ATeamAsync();
        using var adminClient = await SignInAsync(AdminEmail);
        await CreateLabelAsync(adminClient, "WEB", "bug");

        using var memberClient = await SignInAsync(MemberEmail);

        using var response = await memberClient.GetAsync("/api/teams/WEB/labels", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AMemberCannotCreateLabels()
    {
        await ATeamAsync();
        using var client = await SignInAsync(MemberEmail);

        using var response = await client.PostAsJsonAsync(
            "/api/teams/WEB/labels",
            new { name = "bug" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AnOutsiderDoesNotSeeTheLabels()
    {
        await ATeamAsync();
        await AuthenticationScenario.CreateUserAsync(_factory, OutsiderEmail);

        using var client = await AuthenticationScenario.SignInAsync(_factory, OutsiderEmail);

        using var response = await client.GetAsync("/api/teams/WEB/labels", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ALabelCanBeEdited()
    {
        await ATeamAsync();
        using var client = await SignInAsync(AdminEmail);

        var label = await CreateLabelAsync(client, "WEB", "bug");

        using var response = await client.PutAsJsonAsync(
            $"/api/teams/WEB/labels/{label.Id}",
            new { name = "defecto", description = "Renombrada", color = "#4CB782" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<LabelResponse>();

        Assert.Equal("defecto", updated!.Name);
        Assert.Equal("#4CB782", updated.Color);
    }

    [Fact]
    public async Task ALabelOfAnotherTeamCannotBeEdited()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id);
        await TeamScenario.CreateTeamAsync(_factory, "CORE", owner.Id);

        using var client = await AuthenticationScenario.SignInAsync(_factory, OwnerEmail);

        var label = await CreateLabelAsync(client, "CORE", "deuda");

        // Se pide por la ruta de WEB una label que es de CORE.
        using var response = await client.PutAsJsonAsync(
            $"/api/teams/WEB/labels/{label.Id}",
            new { name = "robada" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ALabelCanBeDeleted()
    {
        await ATeamAsync();
        using var client = await SignInAsync(AdminEmail);

        var label = await CreateLabelAsync(client, "WEB", "bug");

        using var deleted = await client.DeleteAsync(
            $"/api/teams/WEB/labels/{label.Id}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var labels = await client.GetFromJsonAsync<PagedResult<LabelResponse>>(
            "/api/teams/WEB/labels", CancellationToken.None);

        Assert.Empty(labels!.Items);
    }

    [Fact]
    public async Task AMemberCannotDeleteLabels()
    {
        await ATeamAsync();
        using var adminClient = await SignInAsync(AdminEmail);
        var label = await CreateLabelAsync(adminClient, "WEB", "bug");

        using var memberClient = await SignInAsync(MemberEmail);

        using var response = await memberClient.DeleteAsync(
            $"/api/teams/WEB/labels/{label.Id}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeletingTheTeamRemovesItsLabels()
    {
        await ATeamAsync();
        using var client = await SignInAsync(OwnerEmail);

        await CreateLabelAsync(client, "WEB", "bug");

        using var deleted = await client.DeleteAsync("/api/teams/WEB", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<Web.Infrastructure.Persistence.AppDbContext>();

        Assert.Empty(dbContext.Labels);
    }

    [Fact]
    public async Task AnInvalidColorIsRejected()
    {
        await ATeamAsync();
        using var client = await SignInAsync(AdminEmail);

        using var response = await client.PostAsJsonAsync(
            "/api/teams/WEB/labels",
            new { name = "bug", color = "rojo" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ALabelWithoutANameIsRejected()
    {
        await ATeamAsync();
        using var client = await SignInAsync(AdminEmail);

        using var response = await client.PostAsJsonAsync(
            "/api/teams/WEB/labels",
            new { name = "" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WithoutASessionTheLabelsApiRespondsUnauthorized()
    {
        using var client = AuthenticationScenario.CreateClient(_factory);

        using var response = await client.GetAsync("/api/teams/WEB/labels", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task ATeamAsync()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        var admin = await AuthenticationScenario.CreateUserAsync(_factory, AdminEmail);
        var member = await AuthenticationScenario.CreateUserAsync(_factory, MemberEmail);

        var team = await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id, "Web");

        await TeamScenario.AddMemberAsync(_factory, team.Id, admin.Id, TeamRole.Admin);
        await TeamScenario.AddMemberAsync(_factory, team.Id, member.Id, TeamRole.Member);
    }

    private Task<HttpClient> SignInAsync(string email) =>
        AuthenticationScenario.SignInAsync(_factory, email);

    private static async Task<LabelResponse> CreateLabelAsync(HttpClient client, string teamKey, string name)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{teamKey}/labels",
            new { name },
            CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<LabelResponse>())!;
    }
}
