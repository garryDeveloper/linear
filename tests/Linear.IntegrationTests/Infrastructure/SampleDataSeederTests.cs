using Linear.Domain.Teams;
using Linear.Domain.Users;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Linear.IntegrationTests.Infrastructure;

/// <summary>
/// Verifica el juego de datos de ejemplo contra la base real.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class SampleDataSeederTests : IAsyncLifetime
{
    private const string AdminEmail = "admin@linear.local";
    private const string SamplePassword = "Linear-Test-1234";

    private readonly PostgresFixture _postgres;
    private readonly DatabaseWebApplicationFactory _factory;

    public SampleDataSeederTests(PostgresFixture postgres)
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
    public async Task TheSeederCreatesUsersAndTeams()
    {
        await SeedAsync();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(5, await dbContext.Users.CountAsync());
        Assert.Equal(3, await dbContext.Teams.CountAsync());
    }

    [Fact]
    public async Task EveryTeamHasAnOwnerAndItsMembers()
    {
        await SeedAsync();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var teams = await dbContext.Teams
            .AsNoTracking()
            .Include(team => team.Members)
            .ToArrayAsync();

        Assert.All(teams, team =>
        {
            Assert.Contains(team.Members, member => member.Role == TeamRole.Owner);
            Assert.True(team.Members.Count > 1);
        });
    }

    [Fact]
    public async Task RunningItTwiceDoesNotDuplicateAnything()
    {
        await SeedAsync();
        await SeedAsync();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(5, await dbContext.Users.CountAsync());
        Assert.Equal(3, await dbContext.Teams.CountAsync());
    }

    [Fact]
    public async Task TheSampleAccountsCanSignIn()
    {
        await SeedAsync();

        using var client = AuthenticationScenario.CreateClient(_factory);

        using var response = await AuthenticationScenario.PostLoginAsync(
            client, "ana.perez@linear.dev", SamplePassword);

        Assert.True(AuthenticationScenario.HasSessionCookie(response));
    }

    [Fact]
    public async Task TheDeactivatedSampleAccountCannotSignIn()
    {
        await SeedAsync();

        using var client = AuthenticationScenario.CreateClient(_factory);

        using var response = await AuthenticationScenario.PostLoginAsync(
            client, "elena.vargas@linear.dev", SamplePassword);

        Assert.False(AuthenticationScenario.HasSessionCookie(response));
    }

    [Fact]
    public async Task TheAdminAccountEndsUpWithTheThreeLevelsOfPermission()
    {
        // Es el objetivo del reparto de roles: poder recorrer los tres niveles sin
        // cambiar de sesión.
        await CreateAdminAsync();
        await SeedAsync();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var adminEmail = Email.Create(AdminEmail).Value;
        var admin = await dbContext.Users.AsNoTracking().FirstAsync(user => user.Email == adminEmail);

        var roles = await dbContext.Teams
            .AsNoTracking()
            .Include(team => team.Members)
            .ToArrayAsync();

        var adminRoles = roles
            .Select(team => team.RoleOf(admin.Id))
            .Where(role => role is not null)
            .ToArray();

        Assert.Contains(TeamRole.Owner, adminRoles);
        Assert.Contains(TeamRole.Admin, adminRoles);
        Assert.Contains(TeamRole.Member, adminRoles);
    }

    [Fact]
    public async Task WithoutTheAdminAccountTheTeamsAreCreatedAnyway()
    {
        await SeedAsync();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(3, await dbContext.Teams.CountAsync());
    }

    [Fact]
    public async Task WithTheSwitchOffNothingIsSeeded()
    {
        await SeedAsync(sampleData: false);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(0, await dbContext.Users.CountAsync());
    }

    [Fact]
    public async Task InProductionNothingIsSeeded()
    {
        await SeedAsync(environmentName: Environments.Production);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(0, await dbContext.Users.CountAsync());
    }

    private async Task CreateAdminAsync() =>
        await AuthenticationScenario.CreateUserAsync(_factory, AdminEmail, UserRole.Admin);

    private async Task SeedAsync(
        bool sampleData = true,
        string? environmentName = null)
    {
        using var scope = _factory.Services.CreateScope();

        var seeder = new SampleDataSeeder(
            scope.ServiceProvider.GetRequiredService<AppDbContext>(),
            scope.ServiceProvider.GetRequiredService<IPasswordHasher>(),
            Options.Create(new SeedOptions
            {
                SampleData = sampleData,
                SamplePassword = SamplePassword,
                AdminEmail = AdminEmail
            }),
            new FakeHostEnvironment(environmentName ?? Environments.Development),
            NullLogger<SampleDataSeeder>.Instance);

        await seeder.SeedAsync(CancellationToken.None);
    }

    /// <summary>
    /// Permite fijar el entorno sin levantar una aplicación distinta por cada caso.
    /// </summary>
    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Linear.Web";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
