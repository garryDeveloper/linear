namespace Linear.Web.Infrastructure.Persistence;

/// <summary>
/// Datos que se siembran al arrancar.
/// </summary>
/// <remarks>
/// Son dos cosas distintas con interruptores separados. <see cref="Enabled"/> crea la
/// cuenta administradora: sin ella no habría forma de iniciar sesión en una instalación
/// nueva, porque todavía no existe el alta de usuarios. <see cref="SampleData"/> agrega
/// un juego de datos de ejemplo para poder recorrer la aplicación con contenido; eso no
/// hace falta para operar, solo para desarrollar y demostrar.
/// </remarks>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>Desactivado por omisión: sembrar es una decisión explícita del entorno.</summary>
    public bool Enabled { get; set; }

    public string AdminEmail { get; set; } = string.Empty;

    public string AdminName { get; set; } = string.Empty;

    public string AdminPassword { get; set; } = string.Empty;

    /// <summary>
    /// Crea usuarios y equipos de ejemplo.
    /// </summary>
    /// <remarks>
    /// Es independiente de <see cref="Enabled"/>: se puede pedir solo el juego de ejemplo
    /// sobre una base que ya tiene cuentas reales, y al revés.
    /// </remarks>
    public bool SampleData { get; set; }

    /// <summary>Contraseña común a todas las cuentas de ejemplo.</summary>
    public string SamplePassword { get; set; } = string.Empty;
}
