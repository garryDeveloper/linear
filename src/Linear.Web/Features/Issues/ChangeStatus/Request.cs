using Linear.Domain.Issues;

namespace Linear.Web.Features.Issues.ChangeStatus;

public sealed class ChangeIssueStatusRequest
{
    public string Key { get; set; } = string.Empty;

    public string Identifier { get; set; } = string.Empty;

    public IssueStatus Status { get; set; }
}
