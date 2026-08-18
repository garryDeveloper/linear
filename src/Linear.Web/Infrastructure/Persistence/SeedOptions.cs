namespace Linear.Web.Infrastructure.Persistence;

/// <summary>
/// Datos iniciales para poder entrar a la aplicación en un entorno recién instalado.
/// </summary>
/// <remarks>
/// La task 002 no incluye alta de usuarios ni invitaciones, así que sin una cuenta
/// sembrada no habría forma de iniciar sesión.
/// </remarks>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>Desactivado por omisión: sembrar es una decisión explícita del entorno.</summary>
    public bool Enabled { get; set; }

    public string AdminEmail { get; set; } = string.Empty;

    public string AdminName { get; set; } = string.Empty;

    public string AdminPassword { get; set; } = string.Empty;
}
