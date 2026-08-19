using Linear.Domain.Common;
using Linear.Domain.Users;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Authentication.Login;

/// <summary>
/// Verifica las credenciales de un usuario.
/// </summary>
/// <remarks>
/// Devuelve el usuario, no una sesión: emitir la cookie es responsabilidad de quien atiende
/// el request, porque en Blazor Server solo se puede firmar la sesión mientras existe un
/// <c>HttpContext</c> cuya respuesta no empezó a escribirse.
/// </remarks>
public sealed class LoginHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IPasswordHasher passwordHasher,
    ILogger<LoginHandler> logger)
{
    public async Task<Result<User>> HandleAsync(
        string? email,
        string? password,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var normalizedEmail = Email.Create(email);

        if (normalizedEmail.IsFailure)
        {
            passwordHasher.VerifyDummy(password ?? string.Empty);
            return Result.Failure<User>(UserErrors.InvalidCredentials);
        }

        // Consulta con seguimiento: si el hash quedó viejo, hay que reescribirlo.
        var user = await dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Email == normalizedEmail.Value, cancellationToken);

        if (user is null)
        {
            // Se calcula un hash igual: sin esto, la respuesta para un email inexistente
            // sería visiblemente más rápida y permitiría enumerar cuentas.
            passwordHasher.VerifyDummy(password ?? string.Empty);
            return Result.Failure<User>(UserErrors.InvalidCredentials);
        }

        var verification = passwordHasher.Verify(user, password ?? string.Empty);

        if (verification == PasswordVerification.Failed)
        {
            logger.LogInformation("Intento de inicio de sesión fallido para {Email}.", normalizedEmail.Value.Value);
            return Result.Failure<User>(UserErrors.InvalidCredentials);
        }

        // El estado de la cuenta se informa recién ahora: antes de verificar la contraseña,
        // decir que una cuenta está desactivada ya confirmaría que existe.
        if (!user.IsActive)
        {
            return Result.Failure<User>(UserErrors.Inactive);
        }

        if (verification == PasswordVerification.SuccessRehashNeeded)
        {
            user.ChangePasswordHash(passwordHasher.Hash(user, password!), DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Se actualizó el hash de contraseña de {UserId}.", user.Id);
        }

        return Result.Success(user);
    }
}
