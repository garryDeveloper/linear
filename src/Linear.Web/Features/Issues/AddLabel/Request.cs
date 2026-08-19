namespace Linear.Web.Features.Issues.AddLabel;

public sealed class AddIssueLabelRequest
{
    public string Key { get; set; } = string.Empty;

    public string Identifier { get; set; } = string.Empty;

    public Guid LabelId { get; set; }
}
