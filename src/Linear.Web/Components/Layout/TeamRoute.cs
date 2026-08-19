namespace Linear.Web.Components.Layout;

/// <summary>
/// Extrae la clave de equipo de una ruta, cuando la ruta cae dentro de un equipo.
/// </summary>
/// <remarks>
/// La usan tanto <see cref="TeamSelector"/> como <see cref="AppSidebar"/> para decidir,
/// a partir de la URL actual, si el usuario está navegando dentro de un equipo — y en ese
/// caso cuál. Vive separada de ambos para que las dos no diverjan en cómo interpretan la
/// misma ruta a medida que se agreguen secciones nuevas (Issues, Sprints, Roadmap).
/// </remarks>
internal static class TeamRoute
{
    public static string? KeyFrom(string relativePath)
    {
        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return segments is ["teams", var key, ..] ? key : null;
    }
}
