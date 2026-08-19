namespace Linear.Web.Features.Sprints.AddIssue;

public sealed class AddSprintIssueRequest
{
    public string Key { get; set; } = string.Empty;

    public Guid SprintId { get; set; }

    /// <summary>Identificador legible del issue, por ejemplo <c>WEB-42</c>.</summary>
    public string Identifier { get; set; } = string.Empty;
}
