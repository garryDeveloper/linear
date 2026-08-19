namespace Linear.Web.Features.Sprints.GetById;

public sealed class GetSprintByIdRequest
{
    public string Key { get; set; } = string.Empty;

    public Guid SprintId { get; set; }
}
