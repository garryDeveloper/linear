namespace Linear.Web.Features.Comments.Create;

public sealed class CreateCommentRequest
{
    /// <summary>Clave del equipo, tomada de la ruta.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Identificador del issue, tomado de la ruta.</summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>Contenido en Markdown.</summary>
    public string Content { get; set; } = string.Empty;
}
