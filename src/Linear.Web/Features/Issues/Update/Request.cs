namespace Linear.Web.Features.Issues.Update;

public sealed class UpdateIssueRequest
{
    public string Key { get; set; } = string.Empty;

    public string Identifier { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }
}
