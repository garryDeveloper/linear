using Linear.Domain.Teams;

namespace Linear.Web.Features.Teams.ChangeMemberRole;

public sealed class ChangeTeamMemberRoleRequest
{
    public string Key { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public TeamRole Role { get; set; }
}
