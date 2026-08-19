using Linear.Domain.Issues;

namespace Linear.Web.Features.Issues.ChangePriority;

public sealed class ChangeIssuePriorityRequest
{
    public string Key { get; set; } = string.Empty;

    public string Identifier { get; set; } = string.Empty;

    public IssuePriority Priority { get; set; }
}
