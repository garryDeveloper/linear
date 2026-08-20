using System.Net;
using System.Net.Http.Json;

using Linear.Domain.Teams;
using Linear.IntegrationTests.Infrastructure;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Features.Search.Contracts;

namespace Linear.IntegrationTests.Features.Search;

/// <summary>
/// La búsqueda global, contra PostgreSQL real: el diccionario, los índices GIN y el ranking
/// solo se comportan de verdad contra el motor.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class SearchEndpointTests : IAsyncLifetime
{
    private const string OwnerEmail = "owner@linear.dev";
    private const string OutsiderEmail = "outsider@linear.dev";

    private readonly PostgresFixture _postgres;
    private readonly DatabaseWebApplicationFactory _factory;

    public SearchEndpointTests(PostgresFixture postgres)
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

    // ---- los cuatro campos que pide la task --------------------------------------------

    [Fact]
    public async Task SearchesByIdentifier()
    {
        await AScenarioAsync();

        Assert.Equal(["WEB-1"], await SearchAsync("WEB-1"));
    }

    [Fact]
    public async Task TheIdentifierDoesNotDependOnCasing()
    {
        await AScenarioAsync();

        Assert.Equal(["WEB-1"], await SearchAsync("web-1"));
    }

    [Fact]
    public async Task SearchesByTitle()
    {
        await AScenarioAsync();

        Assert.Equal(["WEB-1"], await SearchAsync("autenticación"));
    }

    [Fact]
    public async Task SearchesByDescription()
    {
        await AScenarioAsync();

        Assert.Equal(["WEB-2"], await SearchAsync("cloudflare"));
    }

    [Fact]
    public async Task SearchesInComments()
    {
        await AScenarioAsync();

        var results = await SearchFullAsync("kubernetes");

        var result = Assert.Single(results);
        Assert.Equal("WEB-3", result.Identifier);

        // El issue no menciona la palabra: apareció por lo que dice un comentario, y la
        // respuesta lo marca para poder explicarlo en la lista.
        Assert.True(result.MatchedInComment);
    }

    [Fact]
    public async Task ADeletedCommentStopsMatching()
    {
        var scenario = await AScenarioAsync();

        using var client = await SignInAsync(OwnerEmail);

        Assert.Equal(["WEB-3"], await SearchAsync("kubernetes"));

        using var deleted = await client.DeleteAsync(
            $"/api/teams/WEB/issues/WEB-3/comments/{scenario.CommentId}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        Assert.Empty(await SearchAsync("kubernetes"));
    }

    // ---- comportamiento del diccionario ------------------------------------------------

    /// <summary>
    /// El buscador responde mientras se escribe, así que una palabra a medias tiene que
    /// encontrar la completa.
    /// </summary>
    [Fact]
    public async Task MatchesByPrefix()
    {
        await AScenarioAsync();

        Assert.Equal(["WEB-1"], await SearchAsync("autent"));
    }

    /// <summary>
    /// El diccionario 'spanish' reduce las palabras a su raíz, así que el singular encuentra
    /// el plural y al revés.
    /// </summary>
    [Fact]
    public async Task MatchesTheStemOfAWord()
    {
        await AScenarioAsync();

        Assert.Equal(["WEB-2"], await SearchAsync("caído"));
    }

    [Fact]
    public async Task SearchDoesNotDependOnCasing()
    {
        await AScenarioAsync();

        Assert.Equal(["WEB-1"], await SearchAsync("AUTENTICACIÓN"));
    }

    /// <summary>
    /// En castellano se escribe sin acentos todo el tiempo. La configuración del diccionario
    /// encadena <c>unaccent</c>, así que da lo mismo cómo se escriba lo buscado y cómo se
    /// haya escrito lo guardado.
    /// </summary>
    [Fact]
    public async Task SearchWithoutAccentsFindsTextWithAccents()
    {
        await AScenarioAsync();

        Assert.Equal(["WEB-1"], await SearchAsync("autenticacion"));
        Assert.Equal(["WEB-1"], await SearchAsync("AUTENTICACION"));
    }

    [Fact]
    public async Task SearchWithAccentsFindsTextWithoutAccents()
    {
        await AScenarioAsync();

        using var client = await SignInAsync(OwnerEmail);
        var issue = await CreateIssueAsync(client, "WEB", "Revisar la paginacion del listado", null);

        Assert.Equal([issue.Identifier], await SearchAsync("paginación"));
    }

    /// <summary>Todas las palabras tienen que aparecer, no alcanza con una.</summary>
    [Fact]
    public async Task SeveralWordsNarrowTheResult()
    {
        await AScenarioAsync();

        Assert.Equal(["WEB-1"], await SearchAsync("arreglar autenticación"));
        Assert.Empty(await SearchAsync("autenticación cloudflare"));
    }

    // ---- relevancia --------------------------------------------------------------------

    /// <summary>
    /// Quien escribe un identificador está pidiendo ese issue, no uno que lo mencione de
    /// pasada.
    /// </summary>
    [Fact]
    public async Task AnIdentifierMatchComesFirst()
    {
        await AScenarioAsync();

        using var client = await SignInAsync(OwnerEmail);
        await CreateIssueAsync(client, "WEB", "Depende de WEB-2 para salir", "Bloqueado por WEB-2");

        var results = await SearchAsync("WEB-2");

        Assert.Equal("WEB-2", results[0]);
    }

    /// <summary>
    /// El título pesa más que la descripción: un issue que nombra el término en el título va
    /// antes que otro que solo lo menciona en el cuerpo.
    /// </summary>
    [Fact]
    public async Task TheTitleWeighsMoreThanTheDescription()
    {
        await AScenarioAsync();

        using var client = await SignInAsync(OwnerEmail);
        var inDescription = await CreateIssueAsync(
            client, "WEB", "Nota suelta", "Algo sobre facturación al pasar");
        var inTitle = await CreateIssueAsync(
            client, "WEB", "Rehacer la facturación", "Sin detalles todavía");

        var results = await SearchAsync("facturación");

        Assert.Equal([inTitle.Identifier, inDescription.Identifier], results);
    }

    // ---- alcance y aislamiento ---------------------------------------------------------

    /// <summary>
    /// La búsqueda es global: cruza todos los equipos del usuario, no solo el que está
    /// abierto.
    /// </summary>
    [Fact]
    public async Task SearchesAcrossEveryTeamOfTheUser()
    {
        await AScenarioAsync();

        var results = await SearchFullAsync("despliegue");

        Assert.Equal(2, results.Count);
        Assert.Contains(results, result => result.TeamKey == "WEB");
        Assert.Contains(results, result => result.TeamKey == "CORE");
    }

    /// <summary>
    /// Un issue de un equipo ajeno no aparece ni buscando su identificador exacto: el
    /// aislamiento lo garantiza la propia consulta.
    /// </summary>
    [Fact]
    public async Task IssuesFromTeamsTheUserDoesNotBelongToNeverAppear()
    {
        await AScenarioAsync();

        await AuthenticationScenario.CreateUserAsync(_factory, OutsiderEmail);

        Assert.Empty(await SearchAsync("autenticación", OutsiderEmail));
        Assert.Empty(await SearchAsync("WEB-1", OutsiderEmail));
        Assert.Empty(await SearchAsync("kubernetes", OutsiderEmail));
    }

    /// <summary>Los archivados quedan afuera, igual que en el listado de issues.</summary>
    [Fact]
    public async Task ArchivedIssuesAreNotSearched()
    {
        await AScenarioAsync();

        using var client = await SignInAsync(OwnerEmail);

        Assert.Equal(["WEB-1"], await SearchAsync("autenticación"));

        using var archived = await client.PostAsJsonAsync(
            "/api/teams/WEB/issues/WEB-1/archive", new { }, CancellationToken.None);
        archived.EnsureSuccessStatusCode();

        Assert.Empty(await SearchAsync("autenticación"));
    }

    // ---- consultas que no valen la pena ------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a")]
    [InlineData("!!!")]
    public async Task AQueryWithNothingToSearchReturnsNothing(string query)
    {
        await AScenarioAsync();

        Assert.Empty(await SearchAsync(query));
    }

    /// <summary>
    /// Los caracteres con significado en tsquery llegan limpios: si se pasaran tal cual,
    /// PostgreSQL rechazaría la consulta con un error de sintaxis.
    /// </summary>
    [Theory]
    [InlineData("autenticación & bug")]
    [InlineData("autenticación | bug")]
    [InlineData("!autenticación")]
    [InlineData("(autenticación")]
    [InlineData("autenticación:*")]
    [InlineData("'autenticación'")]
    public async Task QueriesWithOperatorCharactersDoNotFail(string query)
    {
        await AScenarioAsync();

        using var client = await SignInAsync(OwnerEmail);

        using var response = await client.GetAsync(
            $"/api/search/issues?query={Uri.EscapeDataString(query)}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TheNumberOfResultsIsLimited()
    {
        await AScenarioAsync();

        using var client = await SignInAsync(OwnerEmail);

        for (var index = 0; index < 6; index++)
        {
            await CreateIssueAsync(client, "WEB", $"Reporte de métricas {index}", null);
        }

        using var response = await client.GetAsync(
            "/api/search/issues?query=métricas&limit=3", CancellationToken.None);

        var results = await response.Content.ReadFromJsonAsync<List<SearchResultResponse>>();

        Assert.Equal(3, results!.Count);
    }

    [Fact]
    public async Task WithoutASessionTheSearchApiRespondsUnauthorized()
    {
        using var client = AuthenticationScenario.CreateClient(_factory);

        using var response = await client.GetAsync("/api/search/issues?query=algo", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- escenario ---------------------------------------------------------------------

    private async Task<string[]> SearchAsync(string query, string email = OwnerEmail) =>
        [.. (await SearchFullAsync(query, email)).Select(result => result.Identifier)];

    private async Task<IReadOnlyList<SearchResultResponse>> SearchFullAsync(
        string query,
        string email = OwnerEmail)
    {
        using var client = await SignInAsync(email);

        using var response = await client.GetAsync(
            $"/api/search/issues?query={Uri.EscapeDataString(query)}", CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<List<SearchResultResponse>>())!;
    }

    private Task<HttpClient> SignInAsync(string email) =>
        AuthenticationScenario.SignInAsync(_factory, email);

    /// <summary>
    /// Dos equipos del mismo usuario, con issues que cubren cada campo buscable: título,
    /// descripción y comentario.
    /// </summary>
    private async Task<Scenario> AScenarioAsync()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);

        await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id, "Web");
        await TeamScenario.CreateTeamAsync(_factory, "CORE", owner.Id, "Core Platform");

        using var client = await SignInAsync(OwnerEmail);

        // WEB-1: coincide por el título.
        await CreateIssueAsync(client, "WEB", "Arreglar la autenticación", "Pasa solo con 2FA activo.");

        // WEB-2: coincide por la descripción.
        await CreateIssueAsync(
            client, "WEB", "Revisar el proxy", "El certificado de cloudflare está caído desde ayer.");

        // WEB-3: no menciona kubernetes en ningún lado; lo dice un comentario.
        await CreateIssueAsync(client, "WEB", "Migrar el worker", "Mover el proceso de fondo.");
        var comment = await CreateCommentAsync(client, "WEB", "WEB-3", "Habría que hacerlo en kubernetes.");

        // Un issue en cada equipo con la misma palabra, para probar el alcance global.
        await CreateIssueAsync(client, "WEB", "Automatizar el despliegue", null);
        await CreateIssueAsync(client, "CORE", "Despliegue de la API", null);

        return new Scenario(owner.Id, comment);
    }

    private static async Task<IssueResponse> CreateIssueAsync(
        HttpClient client,
        string teamKey,
        string title,
        string? description)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{teamKey}/issues", new { title, description }, CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<IssueResponse>())!;
    }

    private static async Task<Guid> CreateCommentAsync(
        HttpClient client,
        string teamKey,
        string identifier,
        string content)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{teamKey}/issues/{identifier}/comments", new { content }, CancellationToken.None);

        response.EnsureSuccessStatusCode();

        var comment = await response.Content.ReadFromJsonAsync<Web.Features.Comments.Contracts.CommentResponse>();

        return comment!.Id;
    }

    private sealed record Scenario(Guid OwnerId, Guid CommentId);
}
