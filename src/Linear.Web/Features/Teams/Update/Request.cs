namespace Linear.Web.Features.Teams.Update;

public sealed class UpdateTeamRequest
{
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
