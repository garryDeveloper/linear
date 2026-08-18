using Linear.Domain.Teams;

namespace Linear.Web.Features.Teams.AddMember;

public sealed class AddTeamMemberRequest
{
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Email de una cuenta que ya existe.
    /// </summary>
    /// <remarks>
    /// No hay invitaciones por email en la V1, así que solo pueden sumarse usuarios
    /// que ya estén dados de alta en la instalación.
    /// </remarks>
    public string Email { get; set; } = string.Empty;

    public TeamRole Role { get; set; } = TeamRole.Member;
}
