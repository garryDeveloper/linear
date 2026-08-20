using System.Net;
using System.Net.Http.Json;

using Linear.IntegrationTests.Infrastructure;
using Linear.Web.Features.Issues.Contracts;

namespace Linear.IntegrationTests.Features.Realtime;

/// <summary>
/// La estrategia de conflictos de la task 014.
/// </summary>
/// <remarks>
/// El caso que importa es el que ocurre en la realidad: alguien abre un issue, se toma unos
/// minutos escribiendo, y mientras tanto otra persona lo cambia. Sin control, el que guarda
/// último pisa al primero y nadie se entera. Acá se fija que no.
/// </remarks>
[Collection(PostgresCollection.Name)]
public sealed class IssueConcurrencyTests : IAsyncLifetime
{
    private const string OwnerEmail = "owner@linear.dev";
    private const string MemberEmail = "member@linear.dev";

    private readonly PostgresFixture _postgres;
    private readonly DatabaseWebApplicationFactory _factory;

    public IssueConcurrencyTests(PostgresFixture postgres)
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

    /// <summary>Guardar sobre la versión que se tenía a la vista funciona.</summary>
    [Fact]
    public async Task SavingWithTheVersionYouSawWorks()
    {
        var (key, issue, client) = await AnIssueAsync();

        var response = await UpdateAsync(client, key, issue.Identifier, "Otro título", issue.UpdatedAt);

        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.Equal("Otro título", response.Issue!.Title);
    }

    /// <summary>
    /// El caso del conflicto: la segunda persona guarda con una versión vieja y se la rechaza
    /// en lugar de pisar lo que escribió la primera.
    /// </summary>
    [Fact]
    public async Task SavingOverSomeoneElsesChangeIsRejected()
    {
        var (key, issue, first) = await AnIssueAsync(withMember: true);

        // La segunda persona ya tenía el issue cargado con esta versión.
        var staleVersion = issue.UpdatedAt;

        using var second = await AuthenticationScenario.SignInAsync(_factory, MemberEmail);

        var theirs = await UpdateAsync(second, key, issue.Identifier, "Lo edité yo primero", staleVersion);

        Assert.Equal(HttpStatusCode.OK, theirs.Status);

        var mine = await UpdateAsync(first, key, issue.Identifier, "Y yo escribí otra cosa", staleVersion);

        Assert.Equal(HttpStatusCode.Conflict, mine.Status);

        // Y lo que quedó guardado es lo de la primera, intacto.
        var current = await ReadAsync(first, key, issue.Identifier);

        Assert.Equal("Lo edité yo primero", current.Title);
    }

    /// <summary>
    /// Omitir la versión equivale a "guardá igual". Es lo razonable para un cliente de API
    /// que no mostró nada antes de escribir; la interfaz siempre la manda.
    /// </summary>
    [Fact]
    public async Task WithoutAVersionTheSaveGoesThrough()
    {
        var (key, issue, client) = await AnIssueAsync(withMember: true);

        using var second = await AuthenticationScenario.SignInAsync(_factory, MemberEmail);

        await UpdateAsync(second, key, issue.Identifier, "Lo edité yo primero", issue.UpdatedAt);

        var mine = await UpdateAsync(client, key, issue.Identifier, "Sin versión", expected: null);

        Assert.Equal(HttpStatusCode.OK, mine.Status);
    }

    /// <summary>
    /// Después de guardar, la respuesta trae la versión nueva: con esa se puede seguir
    /// editando sin que el próximo guardado se rechace.
    /// </summary>
    [Fact]
    public async Task TheResponseCarriesTheNewVersion()
    {
        var (key, issue, client) = await AnIssueAsync();

        var first = await UpdateAsync(client, key, issue.Identifier, "Primera", issue.UpdatedAt);

        Assert.Equal(HttpStatusCode.OK, first.Status);

        var second = await UpdateAsync(client, key, issue.Identifier, "Segunda", first.Issue!.UpdatedAt);

        Assert.Equal(HttpStatusCode.OK, second.Status);
    }

    // ---- andamiaje ----------------------------------------------------------------------

    private async Task<(string Key, IssueResponse Issue, HttpClient Client)> AnIssueAsync(bool withMember = false)
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        var team = await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id, "Web");

        if (withMember)
        {
            var member = await AuthenticationScenario.CreateUserAsync(_factory, MemberEmail);
            await TeamScenario.AddMemberAsync(_factory, team.Id, member.Id, Linear.Domain.Teams.TeamRole.Member);
        }

        var client = await AuthenticationScenario.SignInAsync(_factory, OwnerEmail);

        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{team.Key.Value}/issues",
            new { title = "Arreglar el login" });

        response.EnsureSuccessStatusCode();

        var issue = (await response.Content.ReadFromJsonAsync<IssueResponse>())!;

        return (team.Key.Value, issue, client);
    }

    private static async Task<(HttpStatusCode Status, IssueResponse? Issue)> UpdateAsync(
        HttpClient client,
        string key,
        string identifier,
        string title,
        DateTimeOffset? expected)
    {
        using var response = await client.PutAsJsonAsync(
            $"/api/teams/{key}/issues/{identifier}",
            new { title, expectedUpdatedAt = expected });

        var issue = response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<IssueResponse>()
            : null;

        return (response.StatusCode, issue);
    }

    private static async Task<IssueResponse> ReadAsync(HttpClient client, string key, string identifier)
    {
        using var response = await client.GetAsync($"/api/teams/{key}/issues/{identifier}");

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<IssueResponse>())!;
    }
}
