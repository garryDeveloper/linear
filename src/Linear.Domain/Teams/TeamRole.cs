namespace Linear.Domain.Teams;

/// <summary>
/// Rol de un usuario dentro de un equipo.
/// </summary>
/// <remarks>
/// Es independiente de <see cref="Linear.Domain.Users.UserRole"/>, que describe qué puede
/// hacer alguien en la instalación completa. Un administrador de la instalación no obtiene
/// por eso ningún permiso sobre un equipo al que no pertenece.
/// El orden de los valores expresa jerarquía: un rol mayor incluye lo que puede el menor.
/// </remarks>
public enum TeamRole
{
    /// <summary>Usa el equipo: crea y edita issues.</summary>
    Member = 0,

    /// <summary>Además administra los miembros y la configuración del equipo.</summary>
    Admin = 1,

    /// <summary>Además puede eliminar el equipo y designar otros Owner.</summary>
    Owner = 2
}
