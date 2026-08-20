using System.Text.Json.Serialization;


namespace Linear.Web.Components.Common;

/// <summary>
/// Un atajo de teclado de la aplicación.
/// </summary>
/// <remarks>
/// La tabla vive en C# y se le pasa al motor de JavaScript al registrarlo, en vez de estar
/// escrita de los dos lados: así la pantalla de ayuda y lo que realmente responde el teclado
/// no pueden divergir. Agregar un atajo es agregar una fila acá.
/// </remarks>
public sealed record AppShortcut
{
    /// <summary>Identificador que JavaScript devuelve cuando el atajo se dispara.</summary>
    public required string Id { get; init; }

    /// <summary>Tecla final del atajo, en minúscula.</summary>
    public required string Key { get; init; }

    /// <summary>
    /// Tecla previa de una secuencia, si la hay: en <c>G</c> luego <c>I</c>, es la <c>G</c>.
    /// </summary>
    public string? Chord { get; init; }

    /// <summary>Si pide Ctrl (o Cmd en Mac).</summary>
    public bool Ctrl { get; init; }

    /// <summary>
    /// Si puede dispararse mientras se escribe en un campo de texto.
    /// </summary>
    /// <remarks>
    /// Casi ninguno: la task 013 pide justamente que los atajos no interfieran con la
    /// escritura. La excepción son los que se diseñaron para funcionar dentro del editor.
    /// </remarks>
    public bool AllowInEditor { get; init; }

    /// <summary>
    /// Si lo maneja el motor central. Los que no, se listan igual en la ayuda porque el
    /// usuario los usa, pero los resuelve el control que tiene el foco.
    /// </summary>
    public bool Global { get; init; } = true;

    /// <summary>Cómo se escribe el atajo en la ayuda.</summary>
    public required string Display { get; init; }

    /// <summary>Qué hace, para la ayuda.</summary>
    public required string Description { get; init; }
}

/// <summary>
/// Todos los atajos de la aplicación, en el orden en que se muestran en la ayuda.
/// </summary>
public static class AppShortcuts
{
    public const string CreateIssue = "createIssue";
    public const string Search = "search";
    public const string GoIssues = "goIssues";
    public const string GoSprints = "goSprints";
    public const string GoRoadmap = "goRoadmap";
    public const string Help = "help";

    public static readonly IReadOnlyList<AppShortcut> All =
    [
        new()
        {
            Id = CreateIssue,
            Key = "c",
            Display = "C",
            Description = "Crear issue"
        },
        new()
        {
            // La aplicación tiene una sola búsqueda —el diálogo—, así que "/" abre la misma
            // que Ctrl+K en vez de enfocar un campo que no existe.
            Id = Search,
            Key = "/",
            Display = "/",
            Description = "Buscar"
        },
        new()
        {
            Id = Search,
            Key = "k",
            Ctrl = true,
            // Buscar desde adentro de un editor es razonable, y es el atajo que la gente ya
            // tiene incorporado de otras herramientas.
            AllowInEditor = true,
            Display = "Ctrl/⌘ K",
            Description = "Búsqueda global"
        },
        new()
        {
            Id = GoIssues,
            Chord = "g",
            Key = "i",
            Display = "G I",
            Description = "Ir a Issues"
        },
        new()
        {
            Id = GoSprints,
            Chord = "g",
            Key = "s",
            Display = "G S",
            Description = "Ir a Sprints"
        },
        new()
        {
            Id = GoRoadmap,
            Chord = "g",
            Key = "r",
            Display = "G R",
            Description = "Ir a Roadmap"
        },
        new()
        {
            Id = Help,
            Key = "?",
            Display = "?",
            Description = "Ver esta ayuda"
        },

        // Los que siguen no los maneja el motor central: los resuelve el control que tiene el
        // foco, que es donde tienen sentido. Se listan porque el usuario los usa igual.
        new()
        {
            Id = "closeDialog",
            Key = "escape",
            Global = false,
            Display = "Esc",
            Description = "Cerrar el diálogo abierto"
        },
        new()
        {
            Id = "confirm",
            Key = "enter",
            Global = false,
            Display = "Enter",
            Description = "Confirmar el diálogo, o abrir el resultado marcado en la búsqueda"
        },
        new()
        {
            Id = "submitComment",
            Key = "enter",
            Ctrl = true,
            Global = false,
            Display = "Ctrl/⌘ Enter",
            Description = "Enviar el comentario que se está escribiendo"
        },
        new()
        {
            Id = "bold",
            Key = "b",
            Ctrl = true,
            Global = false,
            Display = "Ctrl/⌘ B",
            Description = "Negrita, dentro del editor"
        },
        new()
        {
            Id = "italic",
            Key = "i",
            Ctrl = true,
            Global = false,
            Display = "Ctrl/⌘ I",
            Description = "Itálica, dentro del editor"
        }
    ];

    /// <summary>Los que maneja el motor central de teclado.</summary>
    public static IReadOnlyList<AppShortcut> Global => [.. All.Where(shortcut => shortcut.Global)];

    /// <summary>La tabla tal como viaja al motor de JavaScript.</summary>
    public static IReadOnlyList<ShortcutBinding> Bindings => [.. Global.Select(ShortcutBinding.From)];
}

/// <summary>
/// Un atajo tal como lo recibe el motor de JavaScript.
/// </summary>
/// <remarks>
/// Es a propósito un tipo aparte de <see cref="AppShortcut"/>. Al navegador solo le hace falta
/// con qué comparar la pulsación; el texto de la ayuda se arma del lado del servidor y mandarlo
/// sería peso de más en cada registro. Separarlo, además, evita marcar propiedades a la vez
/// <c>required</c> y fuera de la serialización, que System.Text.Json rechaza.
/// </remarks>
public sealed record ShortcutBinding
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("key")]
    public required string Key { get; init; }

    /// <summary>Va siempre, aun en null: JavaScript lo usa para distinguir una secuencia.</summary>
    [JsonPropertyName("chord")]
    public string? Chord { get; init; }

    [JsonPropertyName("ctrl")]
    public bool Ctrl { get; init; }

    [JsonPropertyName("allowInEditor")]
    public bool AllowInEditor { get; init; }

    public static ShortcutBinding From(AppShortcut shortcut) => new()
    {
        Id = shortcut.Id,
        Key = shortcut.Key,
        Chord = shortcut.Chord,
        Ctrl = shortcut.Ctrl,
        AllowInEditor = shortcut.AllowInEditor
    };
}
