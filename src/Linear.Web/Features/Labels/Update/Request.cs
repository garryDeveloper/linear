namespace Linear.Web.Features.Labels.Update;

public sealed class UpdateLabelRequest
{
    public string Key { get; set; } = string.Empty;

    public Guid LabelId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Color { get; set; }
}
