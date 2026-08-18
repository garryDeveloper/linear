namespace Linear.Web.Infrastructure.Authentication;

/// <summary>
/// Nombres de las políticas de autorización de la aplicación.
/// </summary>
/// <remarks>
/// La política de pertenencia a un equipo (<c>RequireTeamMember</c>) no está acá todavía:
/// necesita la entidad <c>TeamMember</c>, que llega con la task 003. Declararla ahora sin
/// poder evaluarla obligaría a elegir entre una política que siempre permite —un agujero
/// de seguridad— o una que siempre niega.
/// </remarks>
public static class AuthorizationPolicies
{
    /// <summary>Solo usuarios con rol <c>Admin</c> a nivel de la instalación.</summary>
    public const string RequireAdmin = nameof(RequireAdmin);

    /// <summary>Cualquier usuario autenticado y con un rol reconocido.</summary>
    public const string RequireMember = nameof(RequireMember);
}
