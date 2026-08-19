namespace Linear.Web.Features.Sprints.Complete;

public sealed class CompleteSprintRequest
{
    public string Key { get; set; } = string.Empty;

    public Guid SprintId { get; set; }
}
