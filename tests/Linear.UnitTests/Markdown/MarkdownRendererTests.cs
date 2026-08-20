using Linear.Web.Shared.Markdown;

namespace Linear.UnitTests.Markdown;

/// <summary>
/// La sintaxis que la task 012 pide soportar.
/// </summary>
public class MarkdownRendererTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\n")]
    public void NothingRendersToNothing(string? markdown)
    {
        Assert.Equal(string.Empty, MarkdownRenderer.Render(markdown));
    }

    // ---- títulos ---------------------------------------------------------------------------

    [Theory]
    [InlineData("# Uno", "<h1>Uno</h1>")]
    [InlineData("## Dos", "<h2>Dos</h2>")]
    [InlineData("###### Seis", "<h6>Seis</h6>")]
    public void Headings(string markdown, string expected)
    {
        Assert.Equal(expected, MarkdownRenderer.Render(markdown));
    }

    /// <summary>Sin espacio no es un título: "#bug" es texto, como una etiqueta.</summary>
    [Fact]
    public void AHashWithoutASpaceIsText()
    {
        Assert.Equal("<p>#bug</p>", MarkdownRenderer.Render("#bug"));
    }

    /// <summary>Markdown llega hasta seis niveles; más allá es texto.</summary>
    [Fact]
    public void SevenHashesAreNotAHeading()
    {
        Assert.Equal("<p>####### Siete</p>", MarkdownRenderer.Render("####### Siete"));
    }

    // ---- énfasis ---------------------------------------------------------------------------

    [Theory]
    [InlineData("**negrita**", "<p><strong>negrita</strong></p>")]
    [InlineData("__negrita__", "<p><strong>negrita</strong></p>")]
    [InlineData("*itálica*", "<p><em>itálica</em></p>")]
    [InlineData("_itálica_", "<p><em>itálica</em></p>")]
    [InlineData("**fuerte con *suave* adentro**",
        "<p><strong>fuerte con <em>suave</em> adentro</strong></p>")]
    public void Emphasis(string markdown, string expected)
    {
        Assert.Equal(expected, MarkdownRenderer.Render(markdown));
    }

    /// <summary>
    /// Tres asteriscos pegados al final son el caso que este renderizador no desenreda: el
    /// cierre de la itálica y el de la negrita comparten caracteres. Queda documentado acá
    /// para que sea una limitación conocida y no una sorpresa — degrada a texto, sin riesgo.
    /// </summary>
    [Fact]
    public void ThreeStackedDelimitersDegradeToText()
    {
        var html = MarkdownRenderer.Render("**mezcla *anidada***");

        Assert.Equal("<p><strong>mezcla *anidada</strong>*</p>", html);
    }

    /// <summary>Un delimitador sin cerrar es texto: no se arrastra hasta el final.</summary>
    [Fact]
    public void AnUnclosedDelimiterStaysAsText()
    {
        Assert.Equal("<p>sin *cerrar</p>", MarkdownRenderer.Render("sin *cerrar"));
    }

    // ---- código -----------------------------------------------------------------------------

    [Fact]
    public void InlineCode()
    {
        Assert.Equal("<p>usar <code>dotnet build</code></p>", MarkdownRenderer.Render("usar `dotnet build`"));
    }

    /// <summary>Adentro de código, el Markdown no significa nada.</summary>
    [Fact]
    public void MarkdownInsideCodeIsNotInterpreted()
    {
        Assert.Equal("<p><code>**no es negrita**</code></p>", MarkdownRenderer.Render("`**no es negrita**`"));
    }

    [Fact]
    public void CodeBlock()
    {
        var html = MarkdownRenderer.Render("```\nlínea uno\nlínea dos\n```");

        Assert.Equal("<pre><code>línea uno\nlínea dos</code></pre>", html);
    }

    [Fact]
    public void CodeBlockWithLanguage()
    {
        var html = MarkdownRenderer.Render("```csharp\nvar x = 1;\n```");

        Assert.Contains("class=\"language-csharp\"", html, StringComparison.Ordinal);
    }

    /// <summary>Un bloque sin cerrar llega hasta el final en vez de romper el documento.</summary>
    [Fact]
    public void AnUnclosedCodeBlockRunsToTheEnd()
    {
        var html = MarkdownRenderer.Render("```\nsin cerrar");

        Assert.Equal("<pre><code>sin cerrar</code></pre>", html);
    }

    // ---- listas -----------------------------------------------------------------------------

    [Theory]
    [InlineData("- uno\n- dos")]
    [InlineData("* uno\n* dos")]
    [InlineData("+ uno\n+ dos")]
    public void UnorderedList(string markdown)
    {
        Assert.Equal("<ul><li>uno</li><li>dos</li></ul>", MarkdownRenderer.Render(markdown));
    }

    [Fact]
    public void OrderedList()
    {
        Assert.Equal(
            "<ol><li>uno</li><li>dos</li></ol>",
            MarkdownRenderer.Render("1. uno\n2. dos"));
    }

    [Fact]
    public void ListItemsKeepTheirInlineFormatting()
    {
        Assert.Equal(
            "<ul><li><strong>uno</strong></li><li>con <code>código</code></li></ul>",
            MarkdownRenderer.Render("- **uno**\n- con `código`"));
    }

    // ---- citas -------------------------------------------------------------------------------

    [Fact]
    public void Blockquote()
    {
        Assert.Equal("<blockquote><p>una cita</p></blockquote>", MarkdownRenderer.Render("> una cita"));
    }

    [Fact]
    public void ABlockquoteCanHoldOtherBlocks()
    {
        var html = MarkdownRenderer.Render("> - uno\n> - dos");

        Assert.Equal("<blockquote><ul><li>uno</li><li>dos</li></ul></blockquote>", html);
    }

    // ---- enlaces ------------------------------------------------------------------------------

    [Fact]
    public void Link()
    {
        var html = MarkdownRenderer.Render("[Linear](https://linear.app)");

        Assert.Contains("<a href=\"https://linear.app\"", html, StringComparison.Ordinal);
        Assert.Contains(">Linear</a>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ALinkLabelKeepsItsFormatting()
    {
        var html = MarkdownRenderer.Render("[**fuerte**](https://ejemplo.test)");

        Assert.Contains("<strong>fuerte</strong></a>", html, StringComparison.Ordinal);
    }

    // ---- tablas --------------------------------------------------------------------------------

    [Fact]
    public void Table()
    {
        var html = MarkdownRenderer.Render("""
            | Campo | Valor |
            |-------|-------|
            | uno   | 1     |
            | dos   | 2     |
            """);

        Assert.Contains("<table><thead><tr><th>Campo</th><th>Valor</th></tr></thead>", html,
            StringComparison.Ordinal);
        Assert.Contains("<tbody><tr><td>uno</td><td>1</td></tr>", html, StringComparison.Ordinal);
        Assert.Contains("<tr><td>dos</td><td>2</td></tr></tbody></table>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void TableAlignment()
    {
        var html = MarkdownRenderer.Render("""
            | izq | centro | der |
            |:----|:------:|----:|
            | a   | b      | c   |
            """);

        Assert.Contains("style=\"text-align: left\"", html, StringComparison.Ordinal);
        Assert.Contains("style=\"text-align: center\"", html, StringComparison.Ordinal);
        Assert.Contains("style=\"text-align: right\"", html, StringComparison.Ordinal);
    }

    /// <summary>Sin la línea de guiones no hay tabla: una línea con barras es texto.</summary>
    [Fact]
    public void BarsWithoutASeparatorAreNotATable()
    {
        var html = MarkdownRenderer.Render("a | b | c");

        Assert.DoesNotContain("<table>", html, StringComparison.Ordinal);
        Assert.Contains("<p>", html, StringComparison.Ordinal);
    }

    // ---- párrafos -------------------------------------------------------------------------------

    [Fact]
    public void ABlankLineSeparatesParagraphs()
    {
        Assert.Equal("<p>uno</p><p>dos</p>", MarkdownRenderer.Render("uno\n\ndos"));
    }

    /// <summary>
    /// En un gestor de issues la gente escribe renglones cortos esperando que se respeten, así
    /// que el salto simple se conserva en vez de unir las líneas como haría CommonMark.
    /// </summary>
    [Fact]
    public void ASingleNewlineIsKeptAsALineBreak()
    {
        Assert.Equal("<p>uno<br>dos</p>", MarkdownRenderer.Render("uno\ndos"));
    }

    [Fact]
    public void WindowsLineEndingsWork()
    {
        Assert.Equal("<p>uno</p><p>dos</p>", MarkdownRenderer.Render("uno\r\n\r\ndos"));
    }

    /// <summary>Un bloque nuevo corta el párrafo aunque no haya línea en blanco.</summary>
    [Fact]
    public void ABlockInterruptsAParagraph()
    {
        Assert.Equal("<p>texto</p><ul><li>uno</li></ul>", MarkdownRenderer.Render("texto\n- uno"));
    }

    // ---- escapes ----------------------------------------------------------------------------------

    [Fact]
    public void ABackslashMakesTheNextCharacterLiteral()
    {
        Assert.Equal("<p>*no es itálica*</p>", MarkdownRenderer.Render(@"\*no es itálica\*"));
    }

    // ---- documento completo -------------------------------------------------------------------------

    [Fact]
    public void AWholeDocumentRendersInOrder()
    {
        var html = MarkdownRenderer.Render("""
            # Título

            Un párrafo con **negrita** y un [enlace](https://ejemplo.test).

            - uno
            - dos

            > una cita

            ```csharp
            var x = 1;
            ```
            """);

        var order = new[] { "<h1>", "<p>", "<ul>", "<blockquote>", "<pre>" };
        var lastIndex = -1;

        foreach (var tag in order)
        {
            var index = html.IndexOf(tag, StringComparison.Ordinal);

            Assert.True(index > lastIndex, $"«{tag}» quedó fuera de orden en: {html}");
            lastIndex = index;
        }
    }
}
