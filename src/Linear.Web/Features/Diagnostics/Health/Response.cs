namespace Linear.Web.Features.Diagnostics.Health;

/// <summary>
/// Estado operativo de la aplicación.
/// </summary>
/// <param name="Status">
/// <see cref="HealthStatuses.Healthy"/> si todas las dependencias responden,
/// <see cref="HealthStatuses.Degraded"/> si alguna no.
/// </param>
/// <param name="Database">Estado de la conexión a PostgreSQL.</param>
/// <param name="Environment">Entorno de hosting en el que corre la aplicación.</param>
/// <param name="TimestampUtc">Momento en que se tomó la medición.</param>
public sealed record HealthResponse(
    string Status,
    string Database,
    string Environment,
    DateTimeOffset TimestampUtc);

public static class HealthStatuses
{
    public const string Healthy = "Healthy";
    public const string Degraded = "Degraded";
}

public static class DatabaseStatuses
{
    public const string Connected = "Connected";
    public const string Unavailable = "Unavailable";
}
