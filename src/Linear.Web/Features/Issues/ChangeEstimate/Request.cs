namespace Linear.Web.Features.Issues.ChangeEstimate;

public sealed class ChangeIssueEstimateRequest
{
    public string Key { get; set; } = string.Empty;

    public string Identifier { get; set; } = string.Empty;

    public int? Estimate { get; set; }
}
