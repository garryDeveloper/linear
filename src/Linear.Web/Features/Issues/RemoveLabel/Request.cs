namespace Linear.Web.Features.Issues.RemoveLabel;

public sealed class RemoveIssueLabelRequest
{
    public string Key { get; set; } = string.Empty;

    public string Identifier { get; set; } = string.Empty;

    public Guid LabelId { get; set; }
}
