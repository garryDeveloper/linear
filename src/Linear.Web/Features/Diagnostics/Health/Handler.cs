using Linear.Domain.Common;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Diagnostics.Health;

/// <summary>
/// Reporta si la aplicación y sus dependencias están operativas.
/// </summary>
/// <remarks>
/// Una base de datos inaccesible se informa como estado degradado, no como fallo del
/// endpoint: el objetivo de este slice es poder diagnosticar la conexión, y un 500
/// dejaría al diagnóstico sin información.
/// </remarks>
public sealed class HealthHandler(
    IDbContextFactory<AppDbContext> dbContextFactory,
    IHostEnvironment environment,
    ICurrentUser currentUser,
    ILogger<HealthHandler> logger)
{
    public async Task<Result<HealthResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var databaseAvailable = await CanConnectToDatabaseAsync(cancellationToken);

        // El entorno solo para quien tiene sesión: el endpoint es anónimo a propósito —una
        // sonda pregunta antes de que exista cualquier sesión— y decirle a cualquiera que la
        // instalación corre en Development es decirle que tiene encendidos los errores
        // detallados y el registro de datos sensibles.
        var identified = await currentUser.RequireIdAsync(cancellationToken);

        var response = new HealthResponse(
            Status: databaseAvailable ? HealthStatuses.Healthy : HealthStatuses.Degraded,
            Database: databaseAvailable ? DatabaseStatuses.Connected : DatabaseStatuses.Unavailable,
            Environment: identified.IsSuccess ? environment.EnvironmentName : null,
            TimestampUtc: DateTimeOffset.UtcNow);

        return Result.Success(response);
    }

    private async Task<bool> CanConnectToDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await dbContext.CanConnectAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "No se pudo conectar a PostgreSQL.");
            return false;
        }
    }
}
