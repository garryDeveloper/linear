namespace Linear.Web.Features.Issues.Create;

public sealed class CreateIssueRequest
{
    /// <summary>Clave del equipo, tomada de la ruta.</summary>
    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid? AssigneeId { get; set; }

    public IReadOnlyList<Guid> LabelIds { get; set; } = [];
}
