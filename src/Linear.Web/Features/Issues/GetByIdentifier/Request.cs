namespace Linear.Web.Features.Issues.GetByIdentifier;

public sealed class GetIssueByIdentifierRequest
{
    public string Key { get; set; } = string.Empty;

    public string Identifier { get; set; } = string.Empty;
}
