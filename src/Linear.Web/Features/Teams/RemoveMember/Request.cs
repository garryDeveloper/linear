namespace Linear.Web.Features.Teams.RemoveMember;

public sealed class RemoveTeamMemberRequest
{
    public string Key { get; set; } = string.Empty;

    public Guid UserId { get; set; }
}
