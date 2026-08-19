namespace Linear.Web.Features.Sprints.Cancel;

public sealed class CancelSprintRequest
{
    public string Key { get; set; } = string.Empty;

    public Guid SprintId { get; set; }
}
