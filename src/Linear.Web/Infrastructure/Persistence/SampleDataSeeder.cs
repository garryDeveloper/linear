using Linear.Domain.Teams;
using Linear.Domain.Users;
using Linear.Web.Infrastructure.Authentication;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Linear.Web.Infrastructure.Persistence;

/// <summary>
/// Carga un juego de datos de ejemplo: usuarios, equipos y sus membresías.
/// </summary>
/// <remarks>
/// Sirve para recorrer la aplicación con contenido en vez de pantallas vacías, y para
/// probar los permisos sin tener que armar el escenario a mano cada vez.
/// Es idempotente entidad por entidad: vuelve a ejecutarse sin duplicar nada y completa
/// lo que falte, así que sirve tanto sobre una base recién creada como sobre una a la que
/// se le agregaron equipos nuevos entre versiones.
/// </remarks>
public sealed class SampleDataSeeder(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IOptions<SeedOptions> options,
    IHostEnvironment environment,
    ILogger<SampleDataSeeder> logger)
{
    /// <summary>
    /// Cuentas de ejemplo. Todas comparten la contraseña de <see cref="SeedOptions.SamplePassword"/>.
    /// </summary>
    private static readonly SampleUser[] SampleUsers =
    [
        new("ana.perez@linear.dev", "Ana Pérez", UserRole.Member, IsActive: true),
        new("bruno.gimenez@linear.dev", "Bruno Giménez", UserRole.Member, IsActive: true),
        new("carla.rossi@linear.dev", "Carla Rossi", UserRole.Admin, IsActive: true),
        new("diego.molina@linear.dev", "Diego Molina", UserRole.Member, IsActive: true),

        // Cuenta desactivada a propósito: deja a mano el caso de un usuario que no puede
        // iniciar sesión y que tampoco puede sumarse a un equipo.
        new("elena.vargas@linear.dev", "Elena Vargas", UserRole.Member, IsActive: false)
    ];

    /// <summary>
    /// Equipos de ejemplo.
    /// </summary>
    /// <remarks>
    /// El reparto de roles está pensado para que la cuenta administradora quede como Owner
    /// de un equipo, Admin de otro y Member de un tercero: así se pueden ver los tres
    /// niveles de permiso sin cambiar de sesión.
    /// </remarks>
    private static readonly SampleTeam[] SampleTeams =
    [
        new("WEB", "Web", "Sitio público y panel de clientes.",
        [
            new("ana.perez@linear.dev", TeamRole.Owner),
            new(AdminPlaceholder, TeamRole.Admin),
            new("bruno.gimenez@linear.dev", TeamRole.Member),
            new("carla.rossi@linear.dev", TeamRole.Member)
        ]),

        new("CORE", "Core Platform", "Servicios internos y API.",
        [
            new(AdminPlaceholder, TeamRole.Owner),
            new("bruno.gimenez@linear.dev", TeamRole.Admin),
            new("diego.molina@linear.dev", TeamRole.Member)
        ]),

        new("MOBILE", "Mobile", "Aplicaciones iOS y Android.",
        [
            new("carla.rossi@linear.dev", TeamRole.Owner),
            new(AdminPlaceholder, TeamRole.Member),
            new("diego.molina@linear.dev", TeamRole.Member)
        ])
    ];

    /// <summary>
    /// Marca la cuenta administradora, cuyo email es configurable y no se conoce acá.
    /// </summary>
    private const string AdminPlaceholder = "{admin}";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var seed = options.Value;

        if (!seed.SampleData)
        {
            return;
        }

        // Datos inventados y contraseñas conocidas no tienen lugar en producción.
        if (environment.IsProduction())
        {
            logger.LogWarning("Se ignoraron los datos de ejemplo: están deshabilitados en producción.");
            return;
        }

        if (string.IsNullOrWhiteSpace(seed.SamplePassword))
        {
            logger.LogError(
                "No se sembraron datos de ejemplo: falta '{Section}:{Key}'.",
                SeedOptions.SectionName,
                nameof(SeedOptions.SamplePassword));
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var usersByEmail = await EnsureUsersAsync(seed, now, cancellationToken);

        await AddAdminIfPresentAsync(seed, usersByEmail, cancellationToken);

        var createdTeams = await EnsureTeamsAsync(usersByEmail, now, cancellationToken);

        logger.LogInformation(
            "Datos de ejemplo listos: {UserCount} cuentas y {TeamCount} equipos nuevos.",
            usersByEmail.Count,
            createdTeams);
    }

    private async Task<Dictionary<string, Guid>> EnsureUsersAsync(
        SeedOptions seed,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // Se compara contra los value objects y no contra su contenido: Email se persiste
        // con un conversor, y leer dentro del value object no es traducible a SQL.
        var emails = SampleUsers
            .Select(sample => Email.Create(sample.Email).Value)
            .ToArray();

        var existingUsers = await dbContext.Users
            .AsNoTracking()
            .Where(user => emails.Contains(user.Email))
            .ToArrayAsync(cancellationToken);

        var usersByEmail = existingUsers.ToDictionary(
            user => user.Email.Value,
            user => user.Id,
            StringComparer.Ordinal);

        foreach (var sample in SampleUsers)
        {
            if (usersByEmail.ContainsKey(sample.Email))
            {
                continue;
            }

            var email = Email.Create(sample.Email);
            var user = User.Create(email.Value, sample.Name, sample.Role, "pendiente", now);

            user.Value.ChangePasswordHash(passwordHasher.Hash(user.Value, seed.SamplePassword), now);

            if (!sample.IsActive)
            {
                user.Value.Deactivate(now);
            }

            dbContext.Users.Add(user.Value);
            usersByEmail[sample.Email] = user.Value.Id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return usersByEmail;
    }

    /// <summary>
    /// Resuelve el marcador de la cuenta administradora contra el email configurado.
    /// </summary>
    /// <remarks>
    /// Si esa cuenta no existe —porque la siembra de administrador está apagada— los
    /// equipos se crean igual, solo que sin ella.
    /// </remarks>
    private async Task AddAdminIfPresentAsync(
        SeedOptions seed,
        Dictionary<string, Guid> usersByEmail,
        CancellationToken cancellationToken)
    {
        var adminEmail = Email.Create(seed.AdminEmail);

        if (adminEmail.IsFailure)
        {
            return;
        }

        var admin = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Email == adminEmail.Value, cancellationToken);

        if (admin is null)
        {
            logger.LogInformation(
                "No se encontró la cuenta administradora '{Email}': los equipos de ejemplo se crean sin ella.",
                seed.AdminEmail);
            return;
        }

        usersByEmail[AdminPlaceholder] = admin.Id;
    }

    private async Task<int> EnsureTeamsAsync(
        IReadOnlyDictionary<string, Guid> usersByEmail,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var keys = SampleTeams
            .Select(sample => TeamKey.Create(sample.Key).Value)
            .ToArray();

        var existingKeys = await dbContext.Teams
            .AsNoTracking()
            .Where(team => keys.Contains(team.Key))
            .Select(team => team.Key)
            .ToArrayAsync(cancellationToken);

        var existing = existingKeys
            .Select(key => key.Value)
            .ToHashSet(StringComparer.Ordinal);

        var created = 0;

        foreach (var sample in SampleTeams)
        {
            if (existing.Contains(sample.Key))
            {
                continue;
            }

            var team = BuildTeam(sample, usersByEmail, now);

            if (team is null)
            {
                continue;
            }

            dbContext.Teams.Add(team);
            created++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return created;
    }

    private Team? BuildTeam(
        SampleTeam sample,
        IReadOnlyDictionary<string, Guid> usersByEmail,
        DateTimeOffset now)
    {
        // El Owner se fija al crear el equipo, porque el agregado no admite existir sin uno.
        // Si el Owner declarado no puede resolverse —el caso típico es que la siembra de
        // administrador esté apagada— se promueve al primer miembro disponible, para que el
        // juego de datos quede completo igual en lugar de perder un equipo entero.
        var owner =
            sample.Members.FirstOrDefault(member =>
                member.Role == TeamRole.Owner && usersByEmail.ContainsKey(member.Email))
            ?? sample.Members.FirstOrDefault(member => usersByEmail.ContainsKey(member.Email));

        if (owner is null)
        {
            logger.LogWarning(
                "Se omitió el equipo de ejemplo '{Key}': ninguno de sus miembros existe.",
                sample.Key);
            return null;
        }

        var team = Team.Create(
            sample.Name,
            TeamKey.Create(sample.Key).Value,
            sample.Description,
            usersByEmail[owner.Email],
            now);

        if (team.IsFailure)
        {
            logger.LogWarning("Se omitió el equipo de ejemplo '{Key}': {Error}", sample.Key, team.Error);
            return null;
        }

        foreach (var member in sample.Members.Where(member => member != owner))
        {
            if (usersByEmail.TryGetValue(member.Email, out var userId))
            {
                team.Value.AddMember(userId, member.Role, now);
            }
        }

        return team.Value;
    }

    private sealed record SampleUser(string Email, string Name, UserRole Role, bool IsActive);

    private sealed record SampleTeam(string Key, string Name, string Description, SampleMembership[] Members);

    private sealed record SampleMembership(string Email, TeamRole Role);
}
