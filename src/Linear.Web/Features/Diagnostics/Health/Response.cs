namespace Linear.Web.Features.Diagnostics.Health;

/// <summary>
/// Estado operativo de la aplicación.
/// </summary>
/// <param name="Status">
/// <see cref="HealthStatuses.Healthy"/> si todas las dependencias responden,
/// <see cref="HealthStatuses.Degraded"/> si alguna no.
/// </param>
/// <param name="Database">Estado de la conexión a PostgreSQL.</param>
/// <param name="Environment">
/// Entorno de hosting, solo para quien tiene sesión iniciada.
/// </param>
/// <remarks>
/// El endpoint responde sin autenticación, porque una sonda de disponibilidad tiene que
/// poder preguntar antes de que exista cualquier sesión. Por eso el entorno viaja nulo para
/// quien no la tiene: saber que una instalación corre en <c>Development</c> es saber que
/// tiene los errores detallados y el registro de datos sensibles encendidos, y eso no es algo
/// que deba poder averiguar cualquiera que llegue a la URL.
/// </remarks>
/// <param name="TimestampUtc">Momento en que se tomó la medición.</param>
public sealed record HealthResponse(
    string Status,
    string Database,
    string? Environment,
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
