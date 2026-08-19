namespace Linear.Web.Features.Sprints.Start;

public sealed class StartSprintRequest
{
    public string Key { get; set; } = string.Empty;

    public Guid SprintId { get; set; }
}
