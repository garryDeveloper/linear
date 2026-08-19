namespace Linear.Web.Features.Labels.Create;

public sealed class CreateLabelRequest
{
    /// <summary>Clave del equipo, tomada de la ruta.</summary>
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Color hexadecimal. Si viene vacío se usa el color por omisión.</summary>
    public string? Color { get; set; }
}
