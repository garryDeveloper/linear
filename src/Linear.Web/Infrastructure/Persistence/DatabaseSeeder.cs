using Linear.Domain.Users;
using Linear.Web.Infrastructure.Authentication;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Linear.Web.Infrastructure.Persistence;

/// <summary>
/// Crea la cuenta administradora inicial.
/// </summary>
public sealed class DatabaseSeeder(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IOptions<SeedOptions> options,
    IHostEnvironment environment,
    ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var seed = options.Value;

        if (!seed.Enabled)
        {
            return;
        }

        // Una contraseña conocida y escrita en configuración no puede terminar en producción.
        if (environment.IsProduction())
        {
            logger.LogWarning("Se ignoró la siembra de datos: está deshabilitada en producción.");
            return;
        }

        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var email = Email.Create(seed.AdminEmail);

        if (email.IsFailure)
        {
            logger.LogError("No se sembró la cuenta inicial: {Error}", email.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(seed.AdminPassword))
        {
            logger.LogError(
                "No se sembró la cuenta inicial: falta '{Section}:{Key}'.",
                SeedOptions.SectionName,
                nameof(SeedOptions.AdminPassword));
            return;
        }

        var now = DateTimeOffset.UtcNow;

        // El hash necesita el usuario, y el usuario necesita un hash: se crea con un valor
        // provisorio y se reemplaza antes de persistir.
        var user = User.Create(email.Value, seed.AdminName, UserRole.Admin, "pending", now);

        if (user.IsFailure)
        {
            logger.LogError("No se sembró la cuenta inicial: {Error}", user.Error);
            return;
        }

        user.Value.ChangePasswordHash(passwordHasher.Hash(user.Value, seed.AdminPassword), now);

        dbContext.Users.Add(user.Value);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Se creó la cuenta administradora inicial '{Email}'.", email.Value.Value);
    }
}
