namespace Linear.Domain.Users;

/// <summary>
/// Rol de un usuario a nivel de la aplicación.
/// </summary>
/// <remarks>
/// No confundir con el rol dentro de un equipo: la pertenencia a equipos se modela
/// aparte con <c>TeamMember</c> y tiene su propia escala (Owner/Admin/Member).
/// Este rol decide qué puede hacer alguien en la instalación completa.
/// </remarks>
public enum UserRole
{
    /// <summary>Usuario común. Trabaja dentro de los equipos a los que pertenece.</summary>
    Member = 0,

    /// <summary>Administra la instalación.</summary>
    Admin = 1
}
