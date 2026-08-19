namespace Linear.Web.Features.Sprints.RemoveIssue;

public sealed class RemoveSprintIssueRequest
{
    public string Key { get; set; } = string.Empty;

    public Guid SprintId { get; set; }

    public string Identifier { get; set; } = string.Empty;
}
