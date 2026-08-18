namespace Linear.Web.Features.Teams.Create;

public sealed class CreateTeamRequest
{
    public string Name { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string? Description { get; set; }
}
