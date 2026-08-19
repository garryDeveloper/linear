namespace Linear.Web.Features.Sprints.Update;

public sealed class UpdateSprintRequest
{
    public string Key { get; set; } = string.Empty;

    public Guid SprintId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Goal { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }
}
