using Linear.Domain.Common;
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
    ILogger<HealthHandler> logger)
{
    public async Task<Result<HealthResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var databaseAvailable = await CanConnectToDatabaseAsync(cancellationToken);

        var response = new HealthResponse(
            Status: databaseAvailable ? HealthStatuses.Healthy : HealthStatuses.Degraded,
            Database: databaseAvailable ? DatabaseStatuses.Connected : DatabaseStatuses.Unavailable,
            Environment: environment.EnvironmentName,
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
