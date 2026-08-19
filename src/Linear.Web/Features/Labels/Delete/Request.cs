namespace Linear.Web.Features.Labels.Delete;

public sealed class DeleteLabelRequest
{
    public string Key { get; set; } = string.Empty;

    public Guid LabelId { get; set; }
}
