using System.Net;
using System.Net.Http.Json;

using Linear.Domain.Comments;
using Linear.Domain.Teams;
using Linear.IntegrationTests.Infrastructure;
using Linear.Web.Features.Comments.Contracts;
using Linear.Web.Features.Issues.Contracts;
using Linear.Web.Infrastructure.Persistence;
using Linear.Web.Shared.Pagination;

using Microsoft.EntityFrameworkCore;

namespace Linear.IntegrationTests.Features.Comments;

[Collection(PostgresCollection.Name)]
public sealed class CommentsEndpointTests : IAsyncLifetime
{
    private const string OwnerEmail = "owner@linear.dev";
    private const string AdminEmail = "admin@linear.dev";
    private const string MemberEmail = "member@linear.dev";
    private const string OutsiderEmail = "outsider@linear.dev";

    private readonly PostgresFixture _postgres;
    private readonly DatabaseWebApplicationFactory _factory;

    public CommentsEndpointTests(PostgresFixture postgres)
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
    public async Task AMemberCanCommentOnAnIssue()
    {
        var (team, issue) = await ATeamWithAnIssueAsync();
        using var client = await SignInAsync(MemberEmail);

        using var response = await client.PostAsJsonAsync(
            CommentsUrl(team, issue),
            new { content = "Reproduje el bug en staging." },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var comment = await response.Content.ReadFromJsonAsync<CommentResponse>();

        Assert.NotNull(comment);
        Assert.Equal("Reproduje el bug en staging.", comment.Content);
        Assert.Equal("Usuario de prueba", comment.Author.Name);
        Assert.False(comment.IsEdited);
        Assert.True(comment.CanEdit);
        Assert.True(comment.CanDelete);
    }

    [Fact]
    public async Task AnEmptyCommentIsRejected()
    {
        var (team, issue) = await ATeamWithAnIssueAsync();
        using var client = await SignInAsync(MemberEmail);

        using var response = await client.PostAsJsonAsync(
            CommentsUrl(team, issue), new { content = "   " }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ACommentLongerThanTheLimitIsRejected()
    {
        var (team, issue) = await ATeamWithAnIssueAsync();
        using var client = await SignInAsync(MemberEmail);

        using var response = await client.PostAsJsonAsync(
            CommentsUrl(team, issue),
            new { content = new string('a', Comment.MaxContentLength + 1) },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// El contenido viaja y se guarda como Markdown crudo. Es la preparación que pide la
    /// task 006 para que la 012 solo tenga que renderizarlo.
    /// </summary>
    [Fact]
    public async Task TheMarkdownIsStoredWithoutBeingInterpreted()
    {
        const string markdown = "## Hallazgo\n\n- **falla** en `POST /login`\n\n> pasa solo con 2FA";

        var (team, issue) = await ATeamWithAnIssueAsync();
        using var client = await SignInAsync(MemberEmail);

        var comment = await CreateCommentAsync(client, team, issue, markdown);

        Assert.Equal(markdown, comment.Content);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await dbContext.Comments.AsNoTracking().FirstAsync(c => c.Id == comment.Id);

        Assert.Equal(markdown, stored.Content);
    }

    [Fact]
    public async Task CommentsAreListedInChronologicalOrder()
    {
        var (team, issue) = await ATeamWithAnIssueAsync();
        using var client = await SignInAsync(MemberEmail);

        await CreateCommentAsync(client, team, issue, "Primero");
        await CreateCommentAsync(client, team, issue, "Segundo");
        await CreateCommentAsync(client, team, issue, "Tercero");

        var page = await ListCommentsAsync(client, team, issue);

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(["Primero", "Segundo", "Tercero"], page.Items.Select(comment => comment.Content));
    }

    [Fact]
    public async Task CommentsBelongToTheirOwnIssue()
    {
        var (team, issue) = await ATeamWithAnIssueAsync();
        using var client = await SignInAsync(OwnerEmail);

        var other = await CreateIssueAsync(client, team.Key.Value, "Otro issue");

        await CreateCommentAsync(client, team, issue, "Del primero");
        await CreateCommentAsync(client, team, other.Identifier, "Del segundo");

        var first = await ListCommentsAsync(client, team, issue);
        var second = await ListCommentsAsync(client, team, other.Identifier);

        Assert.Equal("Del primero", Assert.Single(first.Items).Content);
        Assert.Equal("Del segundo", Assert.Single(second.Items).Content);
    }

    [Fact]
    public async Task TheAuthorCanEditTheirOwnComment()
    {
        var (team, issue) = await ATeamWithAnIssueAsync();
        using var client = await SignInAsync(MemberEmail);

        var comment = await CreateCommentAsync(client, team, issue, "Primera versión");

        using var response = await client.PutAsJsonAsync(
            CommentUrl(team, issue, comment.Id),
            new { content = "Segunda versión" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<CommentResponse>();

        Assert.Equal("Segunda versión", updated!.Content);
        Assert.True(updated.IsEdited);

        // Editar no mueve la fecha de publicación. Se compara con tolerancia porque el
        // valor de la creación viene en memoria (precisión de 100 ns) y el de la edición
        // ya pasó por PostgreSQL, que guarda timestamptz con precisión de microsegundos.
        Assert.Equal(comment.CreatedAt, updated.CreatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task NobodyElseCanEditAComment_NotEvenAnOwner()
    {
        var (team, issue) = await ATeamWithAnIssueAsync();

        using var memberClient = await SignInAsync(MemberEmail);
        var comment = await CreateCommentAsync(memberClient, team, issue, "Mío");

        using var ownerClient = await SignInAsync(OwnerEmail);

        using var response = await ownerClient.PutAsJsonAsync(
            CommentUrl(team, issue, comment.Id),
            new { content = "Editado por otro" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var stillThere = await ListCommentsAsync(memberClient, team, issue);
        Assert.Equal("Mío", Assert.Single(stillThere.Items).Content);
    }

    [Fact]
    public async Task TheAuthorCanDeleteTheirOwnComment()
    {
        var (team, issue) = await ATeamWithAnIssueAsync();
        using var client = await SignInAsync(MemberEmail);

        var comment = await CreateCommentAsync(client, team, issue, "Me arrepentí");

        using var response = await client.DeleteAsync(
            CommentUrl(team, issue, comment.Id), CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var page = await ListCommentsAsync(client, team, issue);
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task AMemberCannotDeleteSomeoneElsesComment()
    {
        var (team, issue) = await ATeamWithAnIssueAsync();

        using var ownerClient = await SignInAsync(OwnerEmail);
        var comment = await CreateCommentAsync(ownerClient, team, issue, "Del owner");

        using var memberClient = await SignInAsync(MemberEmail);

        using var response = await memberClient.DeleteAsync(
            CommentUrl(team, issue, comment.Id), CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AnAdminCanModerateSomeoneElsesComment()
    {
        var (team, issue) = await ATeamWithAnIssueAsync();

        using var memberClient = await SignInAsync(MemberEmail);
        var comment = await CreateCommentAsync(memberClient, team, issue, "Fuera de lugar");

        using var adminClient = await SignInAsync(AdminEmail);

        using var response = await adminClient.DeleteAsync(
            CommentUrl(team, issue, comment.Id), CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var page = await ListCommentsAsync(memberClient, team, issue);
        Assert.Empty(page.Items);
    }

    /// <summary>
    /// La eliminación es lógica: la fila sigue en la base con <c>DeletedAt</c>, aunque de
    /// cara al usuario el comentario ya no exista.
    /// </summary>
    [Fact]
    public async Task DeletingKeepsTheRowMarkedAsDeleted()
    {
        var (team, issue) = await ATeamWithAnIssueAsync();
        using var client = await SignInAsync(MemberEmail);

        var comment = await CreateCommentAsync(client, team, issue, "Se borra");

        await client.DeleteAsync(CommentUrl(team, issue, comment.Id), CancellationToken.None);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await dbContext.Comments.AsNoTracking().FirstAsync(c => c.Id == comment.Id);

        Assert.NotNull(stored.DeletedAt);
        Assert.Equal("Se borra", stored.Content);
    }

    [Fact]
    public async Task ADeletedCommentCannotBeEditedOrDeletedAgain()
    {
        var (team, issue) = await ATeamWithAnIssueAsync();
        using var client = await SignInAsync(MemberEmail);

        var comment = await CreateCommentAsync(client, team, issue, "Se borra");
        await client.DeleteAsync(CommentUrl(team, issue, comment.Id), CancellationToken.None);

        using var edit = await client.PutAsJsonAsync(
            CommentUrl(team, issue, comment.Id), new { content = "Revivir" }, CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, edit.StatusCode);

        using var deleteAgain = await client.DeleteAsync(
            CommentUrl(team, issue, comment.Id), CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, deleteAgain.StatusCode);
    }

    /// <summary>
    /// Los permisos los calcula el servidor y viajan en cada comentario, para que la
    /// pantalla no vuelva a deducirlos por su cuenta.
    /// </summary>
    [Fact]
    public async Task EachCommentCarriesWhatTheReaderCanDoWithIt()
    {
        var (team, issue) = await ATeamWithAnIssueAsync();

        using var memberClient = await SignInAsync(MemberEmail);
        await CreateCommentAsync(memberClient, team, issue, "Del member");

        using var adminClient = await SignInAsync(AdminEmail);
        await CreateCommentAsync(adminClient, team, issue, "Del admin");

        var asMember = await ListCommentsAsync(memberClient, team, issue);
        var own = asMember.Items.Single(comment => comment.Content == "Del member");
        var others = asMember.Items.Single(comment => comment.Content == "Del admin");

        Assert.True(own.CanEdit);
        Assert.True(own.CanDelete);
        Assert.False(others.CanEdit);
        Assert.False(others.CanDelete);

        var asAdmin = await ListCommentsAsync(adminClient, team, issue);
        var moderated = asAdmin.Items.Single(comment => comment.Content == "Del member");

        Assert.False(moderated.CanEdit);
        Assert.True(moderated.CanDelete);
    }

    [Fact]
    public async Task SomeoneOutsideTheTeamSeesNeitherTheIssueNorItsComments()
    {
        var (team, issue) = await ATeamWithAnIssueAsync();

        using var memberClient = await SignInAsync(MemberEmail);
        var comment = await CreateCommentAsync(memberClient, team, issue, "Interno");

        await AuthenticationScenario.CreateUserAsync(_factory, OutsiderEmail);
        using var outsiderClient = await AuthenticationScenario.SignInAsync(_factory, OutsiderEmail);

        using var list = await outsiderClient.GetAsync(CommentsUrl(team, issue), CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);

        using var create = await outsiderClient.PostAsJsonAsync(
            CommentsUrl(team, issue), new { content = "Colado" }, CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, create.StatusCode);

        using var delete = await outsiderClient.DeleteAsync(
            CommentUrl(team, issue, comment.Id), CancellationToken.None);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    /// <summary>
    /// La búsqueda del comentario está acotada al issue de la ruta, no solo a su id: pedirlo
    /// por el issue equivocado no lo encuentra.
    /// </summary>
    [Fact]
    public async Task ACommentIsNotReachableThroughAnotherIssue()
    {
        var (team, issue) = await ATeamWithAnIssueAsync();
        using var client = await SignInAsync(OwnerEmail);

        var other = await CreateIssueAsync(client, team.Key.Value, "Otro issue");
        var comment = await CreateCommentAsync(client, team, issue, "Del primero");

        using var response = await client.PutAsJsonAsync(
            CommentUrl(team, other.Identifier, comment.Id),
            new { content = "Por la ruta equivocada" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeletingTheIssueRemovesItsComments()
    {
        var (team, issue) = await ATeamWithAnIssueAsync();

        using var client = await SignInAsync(OwnerEmail);
        await CreateCommentAsync(client, team, issue, "Se va con el issue");

        using var deleted = await client.DeleteAsync(
            $"/api/teams/{team.Key.Value}/issues/{issue}", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Empty(dbContext.Comments);
    }

    [Fact]
    public async Task TheListingIsPaginated()
    {
        var (team, issue) = await ATeamWithAnIssueAsync();
        using var client = await SignInAsync(MemberEmail);

        for (var index = 1; index <= 5; index++)
        {
            await CreateCommentAsync(client, team, issue, $"Comentario {index}");
        }

        var first = await ListCommentsAsync(client, team, issue, page: 1, pageSize: 2);

        Assert.Equal(5, first.TotalCount);
        Assert.Equal(["Comentario 1", "Comentario 2"], first.Items.Select(comment => comment.Content));
        Assert.True(first.HasNextPage);

        var last = await ListCommentsAsync(client, team, issue, page: 3, pageSize: 2);

        Assert.Equal(["Comentario 5"], last.Items.Select(comment => comment.Content));
        Assert.False(last.HasNextPage);
    }

    [Fact]
    public async Task WithoutASessionTheCommentsApiRespondsUnauthorized()
    {
        using var client = AuthenticationScenario.CreateClient(_factory);

        using var response = await client.GetAsync("/api/teams/WEB/issues/WEB-1/comments", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string CommentsUrl(Team team, string identifier) =>
        $"/api/teams/{team.Key.Value}/issues/{identifier}/comments";

    private static string CommentUrl(Team team, string identifier, Guid commentId) =>
        $"{CommentsUrl(team, identifier)}/{commentId}";

    private async Task<(Team Team, string Identifier)> ATeamWithAnIssueAsync()
    {
        var owner = await AuthenticationScenario.CreateUserAsync(_factory, OwnerEmail);
        var admin = await AuthenticationScenario.CreateUserAsync(_factory, AdminEmail);
        var member = await AuthenticationScenario.CreateUserAsync(_factory, MemberEmail);

        var team = await TeamScenario.CreateTeamAsync(_factory, "WEB", owner.Id, "Web");

        await TeamScenario.AddMemberAsync(_factory, team.Id, admin.Id, TeamRole.Admin);
        await TeamScenario.AddMemberAsync(_factory, team.Id, member.Id, TeamRole.Member);

        using var client = await AuthenticationScenario.SignInAsync(_factory, OwnerEmail);
        var issue = await CreateIssueAsync(client, team.Key.Value, "Fix session timeout");

        return (team, issue.Identifier);
    }

    private Task<HttpClient> SignInAsync(string email) =>
        AuthenticationScenario.SignInAsync(_factory, email);

    private static async Task<IssueResponse> CreateIssueAsync(HttpClient client, string teamKey, string title)
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/teams/{teamKey}/issues", new { title }, CancellationToken.None);

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
            CommentsUrl(team, identifier), new { content }, CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<CommentResponse>())!;
    }

    private static async Task<PagedResult<CommentResponse>> ListCommentsAsync(
        HttpClient client,
        Team team,
        string identifier,
        int page = 1,
        int pageSize = PageRequest.DefaultPageSize)
    {
        using var response = await client.GetAsync(
            $"{CommentsUrl(team, identifier)}?page={page}&pageSize={pageSize}", CancellationToken.None);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<PagedResult<CommentResponse>>())!;
    }
}
