using System.Text.Json;
using Linear.Web.Components.Common;

namespace Linear.UnitTests.Shortcuts;

/// <summary>
/// La tabla de atajos.
/// </summary>
/// <remarks>
/// Es la única fuente de verdad: la misma lista alimenta el motor de teclado y la pantalla de
/// ayuda. Estos tests fijan lo que la task 013 pide del contenido de esa tabla — que estén
/// todos los atajos, que no se pisen entre sí, y que ninguno se dispare mientras se escribe
/// salvo los pensados para el editor.
/// </remarks>
public class AppShortcutsTests
{
    /// <summary>Los atajos que la task 013 enumera, con la tecla que les asigna.</summary>
    [Theory]
    [InlineData(AppShortcuts.CreateIssue, "c", false, null)]
    [InlineData(AppShortcuts.Search, "/", false, null)]
    [InlineData(AppShortcuts.Search, "k", true, null)]
    [InlineData(AppShortcuts.GoIssues, "i", false, "g")]
    [InlineData(AppShortcuts.GoSprints, "s", false, "g")]
    [InlineData(AppShortcuts.GoRoadmap, "r", false, "g")]
    public void TheShortcutsOfTheTaskAreDefined(string id, string key, bool ctrl, string? chord)
    {
        var shortcut = AppShortcuts.All.SingleOrDefault(
            candidate => candidate.Id == id && candidate.Key == key && candidate.Ctrl == ctrl);

        Assert.NotNull(shortcut);
        Assert.Equal(chord, shortcut.Chord);
        Assert.True(shortcut.Global, $"«{id}» tiene que manejarlo el motor central.");
    }

    /// <summary>La ayuda tiene que existir, porque la task la pide como criterio de aceptación.</summary>
    [Fact]
    public void ThereIsAShortcutForTheHelp()
    {
        var help = Assert.Single(AppShortcuts.All, shortcut => shortcut.Id == AppShortcuts.Help);

        Assert.Equal("?", help.Key);
        Assert.True(help.Global);
    }

    /// <summary>
    /// Escape, Enter y Ctrl+Enter se listan en la ayuda pero no los toma el motor: los
    /// resuelve el control que tiene el foco, que es donde tienen sentido. Un manejador global
    /// de Enter rompería cualquier formulario.
    /// </summary>
    [Theory]
    [InlineData("closeDialog")]
    [InlineData("confirm")]
    [InlineData("submitComment")]
    [InlineData("bold")]
    [InlineData("italic")]
    public void ContextualShortcutsAreDocumentedButNotGlobal(string id)
    {
        var shortcut = Assert.Single(AppShortcuts.All, candidate => candidate.Id == id);

        Assert.False(shortcut.Global);
        Assert.DoesNotContain(shortcut, AppShortcuts.Global);
    }

    /// <summary>
    /// Dos atajos globales no pueden responder a la misma combinación: el motor toma el
    /// primero que coincide, así que un duplicado dejaría uno muerto en silencio.
    /// </summary>
    [Fact]
    public void NoTwoGlobalShortcutsShareACombination()
    {
        var combinations = AppShortcuts.Global
            .Select(shortcut => (shortcut.Chord, shortcut.Key, shortcut.Ctrl))
            .ToArray();

        Assert.Equal(combinations.Length, combinations.Distinct().Count());
    }

    /// <summary>
    /// La regla central de la task: los atajos no deben interferir con la escritura. La única
    /// excepción global es la búsqueda con Ctrl+K, que es lo esperable desde cualquier lado.
    /// </summary>
    [Fact]
    public void OnlyGlobalSearchRunsWhileTyping()
    {
        var allowed = AppShortcuts.Global.Where(shortcut => shortcut.AllowInEditor).ToArray();

        var only = Assert.Single(allowed);

        Assert.Equal(AppShortcuts.Search, only.Id);
        Assert.True(only.Ctrl);
        Assert.Equal("k", only.Key);
    }

    /// <summary>Una secuencia sin segunda tecla, o una tecla en mayúscula, no la reconocería JS.</summary>
    [Fact]
    public void EveryKeyIsLowercase()
    {
        foreach (var shortcut in AppShortcuts.All)
        {
            Assert.Equal(shortcut.Key.ToLowerInvariant(), shortcut.Key);
            Assert.Equal(shortcut.Chord?.ToLowerInvariant(), shortcut.Chord);
        }
    }

    [Fact]
    public void EveryShortcutIsShownInTheHelp()
    {
        foreach (var shortcut in AppShortcuts.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(shortcut.Display));
            Assert.False(string.IsNullOrWhiteSpace(shortcut.Description));
        }
    }

    /// <summary>
    /// Las combinaciones con Ctrl se muestran como «Ctrl/⌘» porque el motor acepta las dos:
    /// en Mac la tecla es Cmd, y la ayuda no debería mentir en ninguna de las dos plataformas.
    /// </summary>
    [Fact]
    public void ShortcutsWithCtrlAreShownForBothPlatforms()
    {
        foreach (var shortcut in AppShortcuts.All.Where(candidate => candidate.Ctrl))
        {
            Assert.Contains("Ctrl/⌘", shortcut.Display, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Los dos caminos para buscar —"/" y Ctrl+K— tienen que terminar en la misma acción: la
    /// aplicación tiene un solo buscador.
    /// </summary>
    [Fact]
    public void BothSearchShortcutsShareTheSameAction()
    {
        var search = AppShortcuts.Global.Where(shortcut => shortcut.Id == AppShortcuts.Search).ToArray();

        Assert.Equal(2, search.Length);
        Assert.Contains(search, shortcut => shortcut.Key == "/" && !shortcut.Ctrl);
        Assert.Contains(search, shortcut => shortcut.Key == "k" && shortcut.Ctrl);
    }

    /// <summary>
    /// El contrato con JavaScript.
    /// </summary>
    /// <remarks>
    /// El motor de teclado lee la tabla ya serializada y busca las propiedades por nombre. Si
    /// alguna cambiara de nombre —o dejara de serializarse— el atajo no fallaría con un error:
    /// simplemente dejaría de responder, en silencio. Por eso se fija acá la forma exacta del
    /// JSON. Las opciones son las mismas que usa Blazor al invocar JavaScript.
    /// </remarks>
    [Fact]
    public void TheTableTravelsToJavaScriptWithTheExpectedShape()
    {
        var json = JsonSerializer.Serialize(
            AppShortcuts.Bindings,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        using var document = JsonDocument.Parse(json);

        JsonValueKind[] booleans = [JsonValueKind.True, JsonValueKind.False];

        foreach (var element in document.RootElement.EnumerateArray())
        {
            // Lo que JavaScript consulta en cada pulsación.
            Assert.False(string.IsNullOrWhiteSpace(element.GetProperty("id").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(element.GetProperty("key").GetString()));
            Assert.Contains(element.GetProperty("ctrl").ValueKind, booleans);
            Assert.Contains(element.GetProperty("allowInEditor").ValueKind, booleans);

            // "chord" viaja siempre: JavaScript lo compara contra null para distinguir un
            // atajo simple de la segunda tecla de una secuencia.
            Assert.True(element.TryGetProperty("chord", out _));
        }

        // Los textos de la ayuda no viajan: se arman del lado del servidor.
        var first = document.RootElement[0];

        Assert.False(first.TryGetProperty("display", out _));
        Assert.False(first.TryGetProperty("description", out _));

        // Y no se pierde ninguno por el camino.
        Assert.Equal(AppShortcuts.Global.Count, document.RootElement.GetArrayLength());
    }
}
