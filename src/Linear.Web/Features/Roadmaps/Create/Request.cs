namespace Linear.Web.Features.Roadmaps.Create;

public sealed class CreateRoadmapRequest
{
    /// <summary>Clave del equipo, tomada de la ruta.</summary>
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
