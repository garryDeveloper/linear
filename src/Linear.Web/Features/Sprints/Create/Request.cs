namespace Linear.Web.Features.Sprints.Create;

public sealed class CreateSprintRequest
{
    /// <summary>Clave del equipo, tomada de la ruta.</summary>
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Goal { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }
}
