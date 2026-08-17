namespace Linear.Web.Shared.Http;

/// <summary>
/// Configuración del cliente HTTP interno que usan los componentes Blazor
/// para consumir los endpoints de la propia aplicación.
/// </summary>
public sealed class ApiClientOptions
{
    public const string SectionName = "Api";

    /// <summary>
    /// Dirección base de la API. Si queda vacía se resuelve en runtime desde las
    /// direcciones reales en las que escucha Kestrel.
    /// Configurarla explícitamente es necesario cuando la aplicación corre detrás
    /// de un proxy inverso, porque en ese caso las direcciones de Kestrel son internas.
    /// </summary>
    public string? BaseAddress { get; set; }

    /// <summary>Timeout de las llamadas al API interno.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
